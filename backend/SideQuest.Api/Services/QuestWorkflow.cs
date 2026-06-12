using SideQuest.Api.Models;

namespace SideQuest.Api.Services;

/// <summary>
/// Pure domain transitions for the bidding loop. Methods mutate the loaded
/// entity graph (quest + slots + bids) but never touch the database — callers
/// load, mutate, then save. This keeps the rules easy to reason about and test.
/// </summary>
public static class QuestWorkflow
{
    /// <summary>True if the quest is still taking bids.</summary>
    public static bool IsAcceptingBids(Quest quest) =>
        quest.Status is QuestStatus.Open or QuestStatus.Filling;

    /// <summary>
    /// Accept a bid at the agreed amount: fills the next open slot, marks the bid
    /// accepted, recomputes the quest status, and auto-declines leftover bids if
    /// the quest just closed. Returns the slot that was filled.
    /// </summary>
    /// <exception cref="InvalidOperationException">No open slot is available.</exception>
    public static QuestSlot AcceptBid(Quest quest, Bid bid, long agreedAmountCents, DateTimeOffset now)
    {
        var slot = quest.Slots.FirstOrDefault(s => s.Status == SlotStatus.Open)
            ?? throw new InvalidOperationException("The quest has no open slots to fill.");

        slot.Status = SlotStatus.Active;
        slot.AssignedQuesterId = bid.QuesterId;
        slot.AcceptedBidId = bid.Id;
        slot.FilledAt = now;

        // Record the agreed price on the bid so the counter/original distinction
        // is preserved for payout. (Escrow capture lands here once Stripe is wired.)
        bid.CounterAmountCents = agreedAmountCents == bid.AmountCents ? bid.CounterAmountCents : agreedAmountCents;
        bid.Status = BidStatus.Accepted;
        bid.UpdatedAt = now;

        RecomputeStatus(quest, now);

        if (quest.Status == QuestStatus.Closed)
            AutoDeclineOpenBids(quest, except: bid, now);

        return slot;
    }

    /// <summary>
    /// Free a filled slot (quester dropped out or was kicked) and reopen the quest
    /// so it returns to the feed.
    /// </summary>
    public static void ReleaseSlot(Quest quest, QuestSlot slot, SlotStatus outcome, DateTimeOffset now)
    {
        if (outcome is not (SlotStatus.Dropped or SlotStatus.Kicked))
            throw new ArgumentException("Release outcome must be Dropped or Kicked.", nameof(outcome));

        slot.Status = SlotStatus.Open;
        slot.AssignedQuesterId = null;
        slot.AcceptedBidId = null;
        slot.FilledAt = null;
        RecomputeStatus(quest, now);
    }

    /// <summary>
    /// Derive the quest status from its slots. Terminal/override states
    /// (Complete, Disputed) are left untouched.
    /// </summary>
    public static void RecomputeStatus(Quest quest, DateTimeOffset now)
    {
        if (quest.Status is QuestStatus.Complete or QuestStatus.Disputed)
            return;

        var openSlots = quest.Slots.Count(s => s.Status == SlotStatus.Open);
        var workingSlots = quest.Slots.Count(s =>
            s.Status is SlotStatus.Active or SlotStatus.Completed);

        quest.Status = openSlots == 0
            ? QuestStatus.Closed
            : workingSlots > 0
                ? QuestStatus.Filling
                : QuestStatus.Open;

        quest.UpdatedAt = now;
    }

    private static void AutoDeclineOpenBids(Quest quest, Bid except, DateTimeOffset now)
    {
        foreach (var b in quest.Bids)
        {
            if (b.Id == except.Id) continue;
            if (b.Status is BidStatus.Pending or BidStatus.Countered)
            {
                b.Status = BidStatus.Declined;
                b.UpdatedAt = now;
            }
        }
    }
}
