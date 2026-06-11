using System.ComponentModel.DataAnnotations;

namespace SideQuest.Api.Models;

/// <summary>
/// A task posted to the marketplace. A quest offers one or more identical
/// <see cref="QuestSlot"/>s that questers bid to fill. When every slot is filled
/// the quest auto-closes (<see cref="QuestStatus.Filled"/>).
/// </summary>
public class Quest
{
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required, MaxLength(120)]
    public string Title { get; set; } = string.Empty;

    [Required, MaxLength(4000)]
    public string Description { get; set; } = string.Empty;

    /// <summary>Poster's asking budget per slot, in the smallest currency unit (cents).</summary>
    public long BudgetCents { get; set; }

    [Required, MaxLength(3)]
    public string Currency { get; set; } = "USD";

    /// <summary>Optional location label (free text) for in-person quests.</summary>
    [MaxLength(160)]
    public string? Location { get; set; }

    /// <summary>Optional deadline by which the work should be completed.</summary>
    public DateTimeOffset? Deadline { get; set; }

    public QuestStatus Status { get; set; } = QuestStatus.Open;

    // Relationships
    public Guid PosterId { get; set; }
    public User? Poster { get; set; }

    public Guid CategoryId { get; set; }
    public Category? Category { get; set; }

    public ICollection<QuestSlot> Slots { get; set; } = new List<QuestSlot>();
    public ICollection<Bid> Bids { get; set; } = new List<Bid>();

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>Total number of slots offered (derived from <see cref="Slots"/>).</summary>
    public int SlotCount => Slots.Count;

    /// <summary>Number of slots not yet filled.</summary>
    public int OpenSlotCount => Slots.Count(s => s.Status == SlotStatus.Open);
}
