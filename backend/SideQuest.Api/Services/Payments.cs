using SideQuest.Api.Models;

namespace SideQuest.Api.Services;

/// <summary>Platform fee configuration (bound from the "Payments" section).</summary>
public class PaymentOptions
{
    /// <summary>Poster fee as a fraction of the quest value (e.g. 0.12 = 12%).</summary>
    public double PosterFeePercent { get; set; } = 0.12;

    /// <summary>Quester fee as a fraction of the payout (e.g. 0.05 = 5%).</summary>
    public double QuesterFeePercent { get; set; } = 0.05;

    /// <summary>Test hook: when set, the mock provider declines captures of this exact amount.</summary>
    public long? MockDeclineAtAmountCents { get; set; }

    public long PosterFeeFor(long amountCents) => (long)Math.Round(amountCents * PosterFeePercent);
    public long QuesterFeeFor(long amountCents) => (long)Math.Round(amountCents * QuesterFeePercent);
}

/// <summary>Stripe configuration (bound from the "Stripe" section).</summary>
public class StripeOptions
{
    /// <summary>Secret API key. When empty, the mock payment provider is used.</summary>
    public string? SecretKey { get; set; }
}

public record EscrowCaptureRequest(
    Guid QuestId,
    Guid SlotId,
    Guid BidId,
    Guid PosterId,
    Guid QuesterId,
    long AmountCents,
    long PosterFeeCents,
    long QuesterFeeCents,
    string Currency);

public record CaptureOutcome(bool Success, string? CaptureRef, string? FailureReason)
{
    public static CaptureOutcome Ok(string reference) => new(true, reference, null);
    public static CaptureOutcome Fail(string reason) => new(false, null, reason);
}

public record PayoutOutcome(bool Success, string? PayoutRef, string? FailureReason)
{
    public static PayoutOutcome Ok(string reference) => new(true, reference, null);
    public static PayoutOutcome Fail(string reason) => new(false, null, reason);
}

/// <summary>
/// Escrow operations: capture (hold) the poster's funds at acceptance, then
/// release to the quester on completion or refund to the poster.
/// </summary>
public interface IPaymentService
{
    /// <summary>True for a real provider (Stripe), false for the in-process mock.</summary>
    bool IsLive { get; }

    Task<CaptureOutcome> CaptureAsync(EscrowCaptureRequest req, CancellationToken ct = default);
    Task<PayoutOutcome> ReleaseAsync(EscrowPayment payment, CancellationToken ct = default);
    Task<PayoutOutcome> RefundAsync(EscrowPayment payment, CancellationToken ct = default);
}

/// <summary>
/// In-process escrow simulation used when no Stripe key is configured. It moves
/// no real money but exercises the full hold → release/refund state machine, so
/// local development and tests run with zero setup.
/// </summary>
public class MockPaymentService : IPaymentService
{
    private readonly PaymentOptions _options;
    private readonly ILogger<MockPaymentService> _log;

    public MockPaymentService(PaymentOptions options, ILogger<MockPaymentService> log)
    {
        _options = options;
        _log = log;
    }

    public bool IsLive => false;

    public Task<CaptureOutcome> CaptureAsync(EscrowCaptureRequest req, CancellationToken ct = default)
    {
        // Simulate a card decline for a configured amount so the failure path is testable.
        if (_options.MockDeclineAtAmountCents is { } declineAt && req.PosterChargeCents() == declineAt)
        {
            _log.LogInformation("Mock escrow capture DECLINED for charge {Cents}c", declineAt);
            return Task.FromResult(CaptureOutcome.Fail("Mock decline (test)."));
        }

        var reference = $"mock_pi_{Guid.NewGuid():N}";
        _log.LogInformation("Mock escrow HELD {Cents}c (ref {Ref})", req.PosterChargeCents(), reference);
        return Task.FromResult(CaptureOutcome.Ok(reference));
    }

    public Task<PayoutOutcome> ReleaseAsync(EscrowPayment payment, CancellationToken ct = default)
    {
        var reference = $"mock_tr_{Guid.NewGuid():N}";
        _log.LogInformation("Mock escrow RELEASED {Cents}c (ref {Ref})", payment.QuesterPayoutCents, reference);
        return Task.FromResult(PayoutOutcome.Ok(reference));
    }

    public Task<PayoutOutcome> RefundAsync(EscrowPayment payment, CancellationToken ct = default)
    {
        var reference = $"mock_rf_{Guid.NewGuid():N}";
        _log.LogInformation("Mock escrow REFUNDED {Cents}c (ref {Ref})", payment.PosterChargeCents, reference);
        return Task.FromResult(PayoutOutcome.Ok(reference));
    }
}

internal static class EscrowCaptureRequestExtensions
{
    public static long PosterChargeCents(this EscrowCaptureRequest req) =>
        req.AmountCents + req.PosterFeeCents;
}
