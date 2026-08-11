namespace SideQuest.Api.Models;

/// <summary>
/// One fillable position within a <see cref="Quest"/>. A quest with three slots
/// can be worked by three different questers in parallel.
/// </summary>
public class QuestSlot
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid QuestId { get; set; }
    public Quest? Quest { get; set; }

    public SlotStatus Status { get; set; } = SlotStatus.Open;

    /// <summary>The quester assigned to this slot once a bid is accepted.</summary>
    public Guid? AssignedQuesterId { get; set; }
    public User? AssignedQuester { get; set; }

    /// <summary>The accepted bid that filled this slot.</summary>
    public Guid? AcceptedBidId { get; set; }
    public Bid? AcceptedBid { get; set; }

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? FilledAt { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }

    // ---- Dispute (per-slot, manual review in V1) ----

    /// <summary>Why this slot was disputed, set when it moves to Disputed.</summary>
    public string? DisputeReason { get; set; }
    public DateTimeOffset? DisputedAt { get; set; }

    // ---- No-show verification, Layer 1: mutual check-in ----

    /// <summary>Set when the quester checks in / marks they've started the work.</summary>
    public DateTimeOffset? CheckedInAt { get; set; }

    /// <summary>Set when the poster confirms the quester showed up.</summary>
    public DateTimeOffset? PosterConfirmedAt { get; set; }

    /// <summary>Set when the poster reports the quester as a no-show.</summary>
    public DateTimeOffset? NoShowReportedAt { get; set; }
}
