using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SideQuest.Api.Data;
using SideQuest.Api.Dtos;
using SideQuest.Api.Models;
using SideQuest.Api.Services;

namespace SideQuest.Api.Controllers;

/// <summary>
/// The bidding + counter-offer loop.
///
/// Flow: a quester submits a bid (Pending). The poster can Accept it (fills a
/// slot at the bid amount), Decline it, or Counter with a new amount (Countered).
/// After a counter the quester Responds — accepting fills a slot at the counter
/// amount, declining ends the bid.
/// </summary>
/// <remarks>
/// Auth is not wired yet. Poster actions are trusted to be the quest's poster,
/// and the "current quester" for submitting/responding is resolved to the first
/// seeded user who is not the poster. Auth0 will replace both.
/// </remarks>
[ApiController]
public class BidsController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IQuestNotifier _notifier;

    public BidsController(AppDbContext db, IQuestNotifier notifier)
    {
        _db = db;
        _notifier = notifier;
    }

    private static DateTimeOffset Now => DateTimeOffset.UtcNow;

    // ---- Quester: submit a bid -------------------------------------------------

    [HttpPost("api/quests/{questId:guid}/bids")]
    public async Task<ActionResult<BidDto>> Submit(Guid questId, [FromBody] CreateBidDto dto)
    {
        var quest = await _db.Quests
            .Include(q => q.Slots)
            .Include(q => q.Bids)
            .FirstOrDefaultAsync(q => q.Id == questId);

        if (quest is null)
            return NotFound("Quest not found.");
        if (!QuestWorkflow.IsAcceptingBids(quest))
            return Conflict("This quest is no longer accepting bids.");

        // TODO(auth): resolve the quester from the access token. Until then, rotate
        // through seeded questers (anyone but the poster) who don't already have an
        // active bid here, so multi-slot quests can be filled by different people.
        var activeBidderIds = quest.Bids
            .Where(b => b.Status is BidStatus.Pending or BidStatus.Countered or BidStatus.Accepted)
            .Select(b => b.QuesterId)
            .ToHashSet();

        var quester = await _db.Users
            .Where(u => u.Id != quest.PosterId)
            .OrderBy(u => u.CreatedAt)
            .ToListAsync();
        var pick = quester.FirstOrDefault(u => !activeBidderIds.Contains(u.Id));
        if (quester.Count == 0)
            return Problem("No quester account available to bid.", statusCode: 500);
        if (pick is null)
            return Conflict("All demo questers already have an active bid on this quest.");

        // Multi-slot quests are fixed-price: a "claim" is always at the quest's
        // budget, regardless of any amount the client sent. Single-slot quests
        // use the quester's proposed amount.
        var amountCents = quest.IsMultiSlot ? quest.BudgetCents : dto.AmountCents;

        var bid = new Bid
        {
            QuestId = quest.Id,
            QuesterId = pick.Id,
            AmountCents = amountCents,
            Message = string.IsNullOrWhiteSpace(dto.Message) ? null : dto.Message.Trim(),
            Status = BidStatus.Pending,
        };
        _db.Bids.Add(bid);
        await _db.SaveChangesAsync();
        await _notifier.BidsChangedAsync(quest.Id);

        bid.Quester = pick;
        return CreatedAtAction(nameof(GetForQuest), new { questId = quest.Id },
            BidDto.FromEntity(bid));
    }

    // ---- Poster: list bids on a quest -----------------------------------------

    [HttpGet("api/quests/{questId:guid}/bids")]
    public async Task<ActionResult<IEnumerable<BidDto>>> GetForQuest(Guid questId)
    {
        if (!await _db.Quests.AnyAsync(q => q.Id == questId))
            return NotFound("Quest not found.");

        var bids = await _db.Bids
            .Include(b => b.Quester)
            .Where(b => b.QuestId == questId)
            .OrderByDescending(b => b.CreatedAt)
            .ToListAsync();

        return Ok(bids.Select(BidDto.FromEntity));
    }

    // ---- Poster: counter a bid -------------------------------------------------

    [HttpPost("api/bids/{bidId:guid}/counter")]
    public async Task<ActionResult<BidDto>> Counter(Guid bidId, [FromBody] CounterBidDto dto)
    {
        var bid = await LoadBidAsync(bidId, includeQuestGraph: true);
        if (bid is null) return NotFound("Bid not found.");

        if (bid.Quest!.IsMultiSlot)
            return Conflict("Multi-slot quests are fixed-price; counter-offers aren't available.");
        if (bid.Status != BidStatus.Pending)
            return Conflict("Only a pending bid can be countered.");

        bid.CounterAmountCents = dto.CounterAmountCents;
        bid.Status = BidStatus.Countered;
        bid.UpdatedAt = Now;
        await _db.SaveChangesAsync();
        await _notifier.BidsChangedAsync(bid.QuestId);

        return Ok(BidDto.FromEntity(bid));
    }

    // ---- Poster: accept a pending bid -----------------------------------------

    [HttpPost("api/bids/{bidId:guid}/accept")]
    public async Task<ActionResult<BidDto>> Accept(Guid bidId)
    {
        var bid = await LoadBidAsync(bidId, includeQuestGraph: true);
        if (bid is null) return NotFound("Bid not found.");

        if (bid.Status != BidStatus.Pending)
            return Conflict("Only a pending bid can be accepted by the poster.");
        if (!QuestWorkflow.IsAcceptingBids(bid.Quest!))
            return Conflict("This quest is no longer accepting bids.");

        // TODO(stripe): capture escrow for bid.AmountCents on acceptance.
        QuestWorkflow.AcceptBid(bid.Quest!, bid, bid.AmountCents, Now);
        await _db.SaveChangesAsync();
        await _notifier.BidsChangedAsync(bid.QuestId);

        return Ok(BidDto.FromEntity(bid));
    }

    // ---- Poster: decline a bid -------------------------------------------------

    [HttpPost("api/bids/{bidId:guid}/decline")]
    public async Task<ActionResult<BidDto>> Decline(Guid bidId)
    {
        var bid = await LoadBidAsync(bidId);
        if (bid is null) return NotFound("Bid not found.");

        if (bid.Status is not (BidStatus.Pending or BidStatus.Countered))
            return Conflict("Only a pending or countered bid can be declined.");

        bid.Status = BidStatus.Declined;
        bid.UpdatedAt = Now;
        await _db.SaveChangesAsync();
        await _notifier.BidsChangedAsync(bid.QuestId);

        return Ok(BidDto.FromEntity(bid));
    }

    // ---- Quester: respond to a counter-offer ----------------------------------

    [HttpPost("api/bids/{bidId:guid}/respond")]
    public async Task<ActionResult<BidDto>> Respond(Guid bidId, [FromBody] RespondCounterDto dto)
    {
        var bid = await LoadBidAsync(bidId, includeQuestGraph: true);
        if (bid is null) return NotFound("Bid not found.");

        if (bid.Status != BidStatus.Countered)
            return Conflict("There is no counter-offer awaiting a response.");

        if (!dto.Accept)
        {
            bid.Status = BidStatus.Declined;
            bid.UpdatedAt = Now;
            await _db.SaveChangesAsync();
            await _notifier.BidsChangedAsync(bid.QuestId);
            return Ok(BidDto.FromEntity(bid));
        }

        if (!QuestWorkflow.IsAcceptingBids(bid.Quest!))
            return Conflict("This quest is no longer accepting bids.");

        // TODO(stripe): capture escrow for the agreed counter amount.
        var agreed = bid.CounterAmountCents ?? bid.AmountCents;
        QuestWorkflow.AcceptBid(bid.Quest!, bid, agreed, Now);
        await _db.SaveChangesAsync();
        await _notifier.BidsChangedAsync(bid.QuestId);

        return Ok(BidDto.FromEntity(bid));
    }

    // ---- helpers --------------------------------------------------------------

    private async Task<Bid?> LoadBidAsync(Guid bidId, bool includeQuestGraph = false)
    {
        var query = _db.Bids.Include(b => b.Quester).AsQueryable();
        if (includeQuestGraph)
        {
            query = query
                .Include(b => b.Quest!).ThenInclude(q => q.Slots)
                .Include(b => b.Quest!).ThenInclude(q => q.Bids);
        }
        return await query.FirstOrDefaultAsync(b => b.Id == bidId);
    }
}
