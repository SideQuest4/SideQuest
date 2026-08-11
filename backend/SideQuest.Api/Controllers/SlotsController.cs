using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SideQuest.Api.Data;
using SideQuest.Api.Dtos;
using SideQuest.Api.Models;
using SideQuest.Api.Services;

namespace SideQuest.Api.Controllers;

/// <summary>
/// Per-slot actions: check-in / no-show verification and disputes. A dispute is
/// scoped to one filled slot so a problem with one quester never freezes the
/// other slots on a multi-slot quest.
/// </summary>
[ApiController]
[Route("api/slots")]
public class SlotsController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IQuestNotifier _notifier;
    private readonly IPaymentService _payments;

    public SlotsController(AppDbContext db, IQuestNotifier notifier, IPaymentService payments)
    {
        _db = db;
        _notifier = notifier;
        _payments = payments;
    }

    /// <summary>The quester marks that they've arrived / started work on the slot.</summary>
    [HttpPost("{slotId:guid}/checkin")]
    public async Task<ActionResult<QuestDetailDto>> CheckIn(Guid slotId, [FromBody] SlotActorDto dto)
    {
        var (quest, slot) = await Load(slotId);
        if (quest is null || slot is null) return NotFound();
        if (slot.Status != SlotStatus.Active)
            return Conflict("Only an active slot can be checked in.");
        if (slot.AssignedQuesterId != dto.UserId)
            return BadRequest("Only the assigned quester can check in.");

        slot.CheckedInAt = DateTimeOffset.UtcNow;
        return await Save(quest);
    }

    /// <summary>The poster confirms the quester actually showed up.</summary>
    [HttpPost("{slotId:guid}/confirm")]
    public async Task<ActionResult<QuestDetailDto>> Confirm(Guid slotId, [FromBody] SlotActorDto dto)
    {
        var (quest, slot) = await Load(slotId);
        if (quest is null || slot is null) return NotFound();
        if (slot.Status != SlotStatus.Active)
            return Conflict("Only an active slot can be confirmed.");
        if (quest.PosterId != dto.UserId)
            return BadRequest("Only the poster can confirm attendance.");

        slot.PosterConfirmedAt = DateTimeOffset.UtcNow;
        return await Save(quest);
    }

    /// <summary>
    /// The poster reports the quester as a no-show. This opens a dispute on the
    /// slot; the recorded check-in state (or lack of it) is the review's evidence.
    /// </summary>
    [HttpPost("{slotId:guid}/no-show")]
    public async Task<ActionResult<QuestDetailDto>> ReportNoShow(Guid slotId, [FromBody] ReportNoShowDto dto)
    {
        var (quest, slot) = await Load(slotId);
        if (quest is null || slot is null) return NotFound();
        if (quest.PosterId != dto.ByUserId)
            return BadRequest("Only the poster can report a no-show.");

        var error = OpenDispute(quest, slot,
            reason: string.IsNullOrWhiteSpace(dto.Reason)
                ? "No-show: the quester did not show up."
                : $"No-show: {dto.Reason.Trim()}");
        if (error is not null) return error;

        slot.NoShowReportedAt = DateTimeOffset.UtcNow;
        return await Save(quest);
    }

    /// <summary>A participant (poster or the slot's quester) disputes the slot.</summary>
    [HttpPost("{slotId:guid}/dispute")]
    public async Task<ActionResult<QuestDetailDto>> Dispute(Guid slotId, [FromBody] OpenSlotDisputeDto dto)
    {
        var (quest, slot) = await Load(slotId);
        if (quest is null || slot is null) return NotFound();

        var isParticipant = dto.ByUserId == quest.PosterId || dto.ByUserId == slot.AssignedQuesterId;
        if (!isParticipant)
            return BadRequest("Only the poster or the slot's quester can dispute it.");

        var error = OpenDispute(quest, slot, dto.Reason.Trim());
        if (error is not null) return error;

        return await Save(quest);
    }

    /// <summary>
    /// Manual-review resolution of a slot dispute. "release" pays the quester;
    /// "refund" returns escrow to the poster, then reopens or cancels the slot.
    /// </summary>
    [HttpPost("{slotId:guid}/dispute/resolve")]
    public async Task<ActionResult<QuestDetailDto>> Resolve(Guid slotId, [FromBody] ResolveSlotDisputeDto dto)
    {
        var outcome = dto.Outcome.Trim().ToLowerInvariant();
        if (outcome != "refund" && outcome != "release")
            return BadRequest("Outcome must be 'refund' or 'release'.");

        var (quest, slot) = await Load(slotId);
        if (quest is null || slot is null) return NotFound();
        if (slot.Status != SlotStatus.Disputed)
            return Conflict("This slot is not under dispute.");

        var payment = quest.EscrowPayments
            .FirstOrDefault(p => p.SlotId == slot.Id && p.Status == EscrowStatus.Held);
        if (payment is null)
            return Conflict("No held escrow was found for this slot.");

        var now = DateTimeOffset.UtcNow;

        if (outcome == "release")
        {
            var res = await _payments.ReleaseAsync(payment);
            if (!res.Success)
                return Problem(detail: $"Payout failed: {res.FailureReason}",
                    statusCode: StatusCodes.Status502BadGateway, title: "Escrow release failed");

            payment.Status = EscrowStatus.Released;
            payment.PayoutRef = res.PayoutRef;
            payment.ReleasedAt = now;

            slot.Status = SlotStatus.Completed;
            slot.CompletedAt = now;
        }
        else // refund
        {
            var slotOutcome = (dto.SlotOutcome ?? "reopen").Trim().ToLowerInvariant();
            if (slotOutcome != "reopen" && slotOutcome != "cancel")
                return BadRequest("For a refund, slotOutcome must be 'reopen' or 'cancel'.");

            var res = await _payments.RefundAsync(payment);
            if (!res.Success)
                return Problem(detail: $"Refund failed: {res.FailureReason}",
                    statusCode: StatusCodes.Status502BadGateway, title: "Escrow refund failed");

            payment.Status = EscrowStatus.Refunded;
            payment.PayoutRef = res.PayoutRef;
            payment.RefundedAt = now;

            if (slotOutcome == "reopen")
            {
                // Fresh slot: clear the assignment and all per-quester markers so
                // another quester can claim it and the quest returns to the feed.
                slot.Status = SlotStatus.Open;
                slot.AssignedQuesterId = null;
                slot.AcceptedBidId = null;
                slot.FilledAt = null;
                slot.CheckedInAt = null;
                slot.PosterConfirmedAt = null;
                slot.NoShowReportedAt = null;
                slot.DisputeReason = null;
                slot.DisputedAt = null;
            }
            else
            {
                slot.Status = SlotStatus.Dropped;
            }
        }

        QuestWorkflow.RecomputeStatus(quest, now);
        return await Save(quest);
    }

    /// <summary>
    /// Shared guard for opening a dispute on a slot. Returns an error result to
    /// short-circuit the caller, or null on success (mutating the slot in place).
    /// </summary>
    private ActionResult? OpenDispute(Quest quest, QuestSlot slot, string reason)
    {
        if (slot.Status == SlotStatus.Disputed)
            return Conflict("This slot is already under dispute.");
        if (slot.Status != SlotStatus.Active)
            return Conflict("Only an active slot with work in progress can be disputed.");

        var hasHeldEscrow = quest.EscrowPayments
            .Any(p => p.SlotId == slot.Id && p.Status == EscrowStatus.Held);
        if (!hasHeldEscrow)
            return Conflict("There is no escrow held on this slot to dispute.");

        var now = DateTimeOffset.UtcNow;
        slot.Status = SlotStatus.Disputed;
        slot.DisputeReason = reason;
        slot.DisputedAt = now;
        quest.UpdatedAt = now;
        return null;
    }

    private async Task<(Quest? quest, QuestSlot? slot)> Load(Guid slotId)
    {
        var quest = await _db.Quests
            .Include(q => q.Category)
            .Include(q => q.Poster).ThenInclude(u => u!.RatingsReceived)
            .Include(q => q.Slots).ThenInclude(s => s.AssignedQuester)
            .Include(q => q.Bids)
            .Include(q => q.EscrowPayments)
            .FirstOrDefaultAsync(q => q.Slots.Any(s => s.Id == slotId));

        return (quest, quest?.Slots.FirstOrDefault(s => s.Id == slotId));
    }

    private async Task<ActionResult<QuestDetailDto>> Save(Quest quest)
    {
        await _db.SaveChangesAsync();
        await _notifier.QuestChangedAsync(quest.Id);
        return Ok(QuestDetailDto.FromEntity(quest));
    }
}
