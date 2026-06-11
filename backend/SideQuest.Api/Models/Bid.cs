using System.ComponentModel.DataAnnotations;

namespace SideQuest.Api.Models;

/// <summary>
/// A quester's offer to take on a <see cref="Quest"/>. Supports a two-way
/// negotiation: the poster may counter with a different amount
/// (<see cref="CounterAmountCents"/>) before the bid is accepted or rejected.
/// </summary>
public class Bid
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid QuestId { get; set; }
    public Quest? Quest { get; set; }

    public Guid QuesterId { get; set; }
    public User? Quester { get; set; }

    /// <summary>Amount the quester proposes, in the smallest currency unit (cents).</summary>
    public long AmountCents { get; set; }

    /// <summary>Poster's counter-offer, if any, in cents.</summary>
    public long? CounterAmountCents { get; set; }

    [MaxLength(1000)]
    public string? Message { get; set; }

    public BidStatus Status { get; set; } = BidStatus.Pending;

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// The currently effective price: the counter-offer once the poster has made
    /// one, otherwise the quester's original amount.
    /// </summary>
    public long EffectiveAmountCents => CounterAmountCents ?? AmountCents;
}
