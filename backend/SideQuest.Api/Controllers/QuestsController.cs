using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SideQuest.Api.Data;
using SideQuest.Api.Dtos;
using SideQuest.Api.Models;
using SideQuest.Api.Services;

namespace SideQuest.Api.Controllers;

[ApiController]
[Route("api/quests")]
public class QuestsController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IQuestNotifier _notifier;
    private readonly IPaymentService _payments;

    public QuestsController(AppDbContext db, IQuestNotifier notifier, IPaymentService payments)
    {
        _db = db;
        _notifier = notifier;
        _payments = payments;
    }

    /// <summary>
    /// Quest feed. Defaults to quests still accepting bids (Open + Filling),
    /// newest first, with optional category, search, and status filters plus
    /// simple paging.
    /// </summary>
    /// <param name="status">
    /// "available" (default) for quests accepting bids, "all" for every quest,
    /// or a specific status name (Open, Filling, Closed, Complete, Disputed).
    /// </param>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<QuestSummaryDto>>> GetFeed(
        [FromQuery] string? category,
        [FromQuery] string? search,
        [FromQuery] string status = "available",
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, 50);

        var query = _db.Quests
            .Include(q => q.Category)
            .Include(q => q.Poster).ThenInclude(u => u!.RatingsReceived)
            .Include(q => q.Slots)
            .Include(q => q.Bids)
            .AsQueryable();

        // Status filter: "available" (default — accepting bids), "all", or a
        // specific QuestStatus name.
        if (string.Equals(status, "available", StringComparison.OrdinalIgnoreCase))
        {
            query = query.Where(q =>
                q.Status == QuestStatus.Open || q.Status == QuestStatus.Filling);
        }
        else if (!string.Equals(status, "all", StringComparison.OrdinalIgnoreCase))
        {
            if (Enum.TryParse<QuestStatus>(status, ignoreCase: true, out var parsed))
                query = query.Where(q => q.Status == parsed);
            else
                return BadRequest($"Unknown status '{status}'.");
        }

        if (!string.IsNullOrWhiteSpace(category))
            query = query.Where(q => q.Category!.Slug == category);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(q =>
                EF.Functions.Like(q.Title, $"%{term}%") ||
                EF.Functions.Like(q.Description, $"%{term}%"));
        }

        var results = await query
            .OrderByDescending(q => q.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return Ok(results.Select(QuestSummaryDto.FromEntity));
    }

    /// <summary>Full detail for a single quest, including its slots.</summary>
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<QuestDetailDto>> GetById(Guid id)
    {
        var quest = await _db.Quests
            .Include(q => q.Category)
            .Include(q => q.Poster).ThenInclude(u => u!.RatingsReceived)
            .Include(q => q.Slots).ThenInclude(s => s.AssignedQuester)
            .Include(q => q.Bids)
            .Include(q => q.EscrowPayments)
            .FirstOrDefaultAsync(q => q.Id == id);

        return quest is null ? NotFound() : Ok(QuestDetailDto.FromEntity(quest));
    }

    /// <summary>
    /// Poster marks the quest complete: every filled slot is closed out and its
    /// escrow is released to the quester. Moves the quest to Complete.
    /// </summary>
    /// <remarks>
    /// Auth is stubbed, so the caller is trusted to be the poster. Auto-release
    /// after a review window and dispute-driven refunds come with the dispute flow.
    /// </remarks>
    [HttpPost("{id:guid}/complete")]
    public async Task<ActionResult<QuestDetailDto>> Complete(Guid id)
    {
        var quest = await _db.Quests
            .Include(q => q.Category)
            .Include(q => q.Poster).ThenInclude(u => u!.RatingsReceived)
            .Include(q => q.Slots).ThenInclude(s => s.AssignedQuester)
            .Include(q => q.Bids)
            .Include(q => q.EscrowPayments)
            .FirstOrDefaultAsync(q => q.Id == id);

        if (quest is null) return NotFound();
        if (quest.Status == QuestStatus.Complete)
            return Conflict("Quest is already complete.");
        if (quest.Status == QuestStatus.Disputed)
            return Conflict("Quest is under dispute and can't be completed.");

        var filledSlots = quest.Slots.Where(s => s.Status == SlotStatus.Active).ToList();
        if (filledSlots.Count == 0)
            return Conflict("There are no filled slots to complete.");

        // Release every held escrow to its quester.
        var now = DateTimeOffset.UtcNow;
        foreach (var payment in quest.EscrowPayments.Where(p => p.Status == EscrowStatus.Held))
        {
            var outcome = await _payments.ReleaseAsync(payment);
            if (!outcome.Success)
                return Problem(
                    detail: $"Payout failed: {outcome.FailureReason}",
                    statusCode: StatusCodes.Status502BadGateway,
                    title: "Escrow release failed");

            payment.Status = EscrowStatus.Released;
            payment.PayoutRef = outcome.PayoutRef;
            payment.ReleasedAt = now;
        }

        foreach (var slot in filledSlots)
        {
            slot.Status = SlotStatus.Completed;
            slot.CompletedAt = now;
        }

        quest.Status = QuestStatus.Complete;
        quest.UpdatedAt = now;

        await _db.SaveChangesAsync();
        await _notifier.QuestChangedAsync(quest.Id);

        return Ok(QuestDetailDto.FromEntity(quest));
    }

    /// <summary>
    /// A participant (the poster or an assigned quester) opens a dispute on an
    /// in-progress quest that still holds escrow, freezing it for manual review.
    /// </summary>
    /// <remarks>Auth is stubbed, so the raiser is taken from the request body.</remarks>
    [HttpPost("{id:guid}/dispute")]
    public async Task<ActionResult<QuestDetailDto>> OpenDispute(Guid id, [FromBody] OpenDisputeDto dto)
    {
        var quest = await LoadQuestGraph(id);
        if (quest is null) return NotFound();

        if (quest.Status == QuestStatus.Disputed)
            return Conflict("This quest is already under dispute.");
        if (quest.Status != QuestStatus.Filling && quest.Status != QuestStatus.Closed)
            return Conflict("Only an in-progress quest with active work can be disputed.");
        if (!quest.EscrowPayments.Any(p => p.Status == EscrowStatus.Held))
            return Conflict("There is no escrow held on this quest to dispute.");

        var isParticipant = dto.ByUserId == quest.PosterId
            || quest.Slots.Any(s => s.AssignedQuesterId == dto.ByUserId);
        if (!isParticipant)
            return BadRequest("Only the poster or an assigned quester can dispute this quest.");

        var now = DateTimeOffset.UtcNow;
        quest.Status = QuestStatus.Disputed;
        quest.DisputeReason = dto.Reason.Trim();
        quest.DisputedAt = now;
        quest.UpdatedAt = now;

        await _db.SaveChangesAsync();
        await _notifier.QuestChangedAsync(quest.Id);
        return Ok(QuestDetailDto.FromEntity(quest));
    }

    /// <summary>
    /// Manual-review resolution of a disputed quest: "refund" returns held
    /// escrow to the poster (in full, no fee), "release" pays the questers.
    /// </summary>
    /// <remarks>Auth is stubbed; this stands in for an admin/moderator action.</remarks>
    [HttpPost("{id:guid}/dispute/resolve")]
    public async Task<ActionResult<QuestDetailDto>> ResolveDispute(Guid id, [FromBody] ResolveDisputeDto dto)
    {
        var outcome = dto.Outcome.Trim().ToLowerInvariant();
        if (outcome != "refund" && outcome != "release")
            return BadRequest("Outcome must be 'refund' or 'release'.");

        var quest = await LoadQuestGraph(id);
        if (quest is null) return NotFound();
        if (quest.Status != QuestStatus.Disputed)
            return Conflict("This quest is not under dispute.");

        var now = DateTimeOffset.UtcNow;
        var held = quest.EscrowPayments.Where(p => p.Status == EscrowStatus.Held).ToList();

        if (outcome == "refund")
        {
            foreach (var payment in held)
            {
                var res = await _payments.RefundAsync(payment);
                if (!res.Success)
                    return Problem(
                        detail: $"Refund failed: {res.FailureReason}",
                        statusCode: StatusCodes.Status502BadGateway,
                        title: "Escrow refund failed");

                payment.Status = EscrowStatus.Refunded;
                payment.PayoutRef = res.PayoutRef;
                payment.ReleasedAt = now;
            }
            foreach (var slot in quest.Slots.Where(s => s.Status == SlotStatus.Active))
                slot.Status = SlotStatus.Dropped;
            quest.Status = QuestStatus.Closed;
        }
        else // release
        {
            foreach (var payment in held)
            {
                var res = await _payments.ReleaseAsync(payment);
                if (!res.Success)
                    return Problem(
                        detail: $"Payout failed: {res.FailureReason}",
                        statusCode: StatusCodes.Status502BadGateway,
                        title: "Escrow release failed");

                payment.Status = EscrowStatus.Released;
                payment.PayoutRef = res.PayoutRef;
                payment.ReleasedAt = now;
            }
            foreach (var slot in quest.Slots.Where(s => s.Status == SlotStatus.Active))
            {
                slot.Status = SlotStatus.Completed;
                slot.CompletedAt = now;
            }
            quest.Status = QuestStatus.Complete;
        }

        quest.UpdatedAt = now;
        await _db.SaveChangesAsync();
        await _notifier.QuestChangedAsync(quest.Id);
        return Ok(QuestDetailDto.FromEntity(quest));
    }

    /// <summary>Loads a quest with the full navigation graph used by detail responses.</summary>
    private Task<Quest?> LoadQuestGraph(Guid id) => _db.Quests
        .Include(q => q.Category)
        .Include(q => q.Poster).ThenInclude(u => u!.RatingsReceived)
        .Include(q => q.Slots).ThenInclude(s => s.AssignedQuester)
        .Include(q => q.Bids)
        .Include(q => q.EscrowPayments)
        .FirstOrDefaultAsync(q => q.Id == id);

    /// <summary>
    /// Create a quest with the requested number of open slots.
    /// </summary>
    /// <remarks>
    /// Auth is not wired yet: the poster defaults to the first seeded user.
    /// Once Auth0 is integrated this will resolve the poster from the access token.
    /// </remarks>
    [HttpPost]
    public async Task<ActionResult<QuestDetailDto>> Create([FromBody] CreateQuestDto dto)
    {
        var category = await _db.Categories.FindAsync(dto.CategoryId);
        if (category is null)
            return BadRequest($"Category '{dto.CategoryId}' does not exist.");

        // TODO(auth): resolve poster from the authenticated Auth0 user.
        var poster = await _db.Users.OrderBy(u => u.CreatedAt).FirstOrDefaultAsync();
        if (poster is null)
            return Problem("No users exist to attribute this quest to.", statusCode: 500);

        var quest = new Quest
        {
            Title = dto.Title.Trim(),
            Description = dto.Description.Trim(),
            BudgetCents = dto.BudgetCents,
            Currency = dto.Currency.ToUpperInvariant(),
            Location = string.IsNullOrWhiteSpace(dto.Location) ? null : dto.Location.Trim(),
            Deadline = dto.Deadline,
            CategoryId = category.Id,
            PosterId = poster.Id,
            Status = QuestStatus.Open,
        };
        for (var i = 0; i < dto.SlotCount; i++)
            quest.Slots.Add(new QuestSlot { Status = SlotStatus.Open });

        _db.Quests.Add(quest);
        await _db.SaveChangesAsync();

        // Reload with navigation properties for the response DTO.
        await _db.Entry(quest).Reference(q => q.Category).LoadAsync();
        await _db.Entry(quest).Reference(q => q.Poster).LoadAsync();

        // Push the new quest onto the live feed.
        await _notifier.QuestChangedAsync(quest.Id);

        return CreatedAtAction(nameof(GetById), new { id = quest.Id },
            QuestDetailDto.FromEntity(quest));
    }
}
