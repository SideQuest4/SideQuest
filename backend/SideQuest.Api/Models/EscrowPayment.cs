using System.ComponentModel.DataAnnotations;

namespace SideQuest.Api.Models;

/// <summary>Lifecycle of money held for a single filled slot.</summary>
public enum EscrowStatus
{
    /// <summary>Funds captured from the poster and held by the platform.</summary>
    Held = 0,

    /// <summary>Funds transferred to the quester (quest completed).</summary>
    Released = 1,

    /// <summary>Funds returned to the poster (cancelled / dispute resolved for poster).</summary>
    Refunded = 2,
}

/// <summary>
/// Escrow for one filled slot. Captured from the poster when a bid is accepted
/// (capture-at-acceptance), held by the platform, and released to the quester on
/// completion — or refunded to the poster. One record per accepted bid / slot.
///
/// Money math (all in cents):
///   poster is charged   = <see cref="AmountCents"/> + <see cref="PosterFeeCents"/>
///   quester is paid out = <see cref="AmountCents"/> - <see cref="QuesterFeeCents"/>
///   platform keeps       = PosterFeeCents + QuesterFeeCents
/// </summary>
public class EscrowPayment
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid QuestId { get; set; }
    public Quest? Quest { get; set; }

    public Guid SlotId { get; set; }
    public QuestSlot? Slot { get; set; }

    public Guid BidId { get; set; }
    public Bid? Bid { get; set; }

    /// <summary>The poster who funds the escrow.</summary>
    public Guid PosterId { get; set; }

    /// <summary>The quester who receives the payout on completion.</summary>
    public Guid QuesterId { get; set; }

    /// <summary>Agreed price for the slot (the quest's value for this slot).</summary>
    public long AmountCents { get; set; }

    /// <summary>Platform fee paid by the poster on top of the amount.</summary>
    public long PosterFeeCents { get; set; }

    /// <summary>Platform fee deducted from the quester's payout.</summary>
    public long QuesterFeeCents { get; set; }

    [Required, MaxLength(3)]
    public string Currency { get; set; } = "USD";

    public EscrowStatus Status { get; set; } = EscrowStatus.Held;

    /// <summary>Provider reference for the capture (e.g. Stripe PaymentIntent id).</summary>
    [MaxLength(64)]
    public string? CaptureRef { get; set; }

    /// <summary>Provider reference for the payout (e.g. Stripe Transfer id).</summary>
    [MaxLength(64)]
    public string? PayoutRef { get; set; }

    public DateTimeOffset CapturedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? ReleasedAt { get; set; }
    public DateTimeOffset? RefundedAt { get; set; }

    /// <summary>Total charged to the poster, in cents.</summary>
    public long PosterChargeCents => AmountCents + PosterFeeCents;

    /// <summary>Net amount paid to the quester, in cents.</summary>
    public long QuesterPayoutCents => AmountCents - QuesterFeeCents;
}
