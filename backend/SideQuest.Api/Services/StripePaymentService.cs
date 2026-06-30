using Microsoft.EntityFrameworkCore;
using SideQuest.Api.Data;
using SideQuest.Api.Models;
using Stripe;

namespace SideQuest.Api.Services;

/// <summary>
/// Real Stripe Connect escrow using the separate charges &amp; transfers pattern:
/// at acceptance the poster's funds are captured to the platform balance; on
/// completion they're transferred to the quester's connected account; a refund
/// returns them to the poster.
/// </summary>
/// <remarks>
/// Active only when a Stripe secret key is configured. Going fully live also
/// requires collecting the poster's payment method (Checkout/Elements) and
/// onboarding questers as connected accounts — those UI flows are the next step;
/// this service is the server-side integration they plug into.
/// </remarks>
public class StripePaymentService : IPaymentService
{
    private readonly AppDbContext _db;
    private readonly ILogger<StripePaymentService> _log;

    public StripePaymentService(StripeOptions options, AppDbContext db, ILogger<StripePaymentService> log)
    {
        _db = db;
        _log = log;
        StripeConfiguration.ApiKey = options.SecretKey;
    }

    public bool IsLive => true;

    public async Task<CaptureOutcome> CaptureAsync(EscrowCaptureRequest req, CancellationToken ct = default)
    {
        try
        {
            // Capture the full poster charge (amount + poster fee) to the platform
            // balance. The transfer to the quester is delayed until completion.
            var intent = await new PaymentIntentService().CreateAsync(new PaymentIntentCreateOptions
            {
                Amount = req.AmountCents + req.PosterFeeCents,
                Currency = req.Currency.ToLowerInvariant(),
                CaptureMethod = "automatic",
                Metadata = new Dictionary<string, string>
                {
                    ["questId"] = req.QuestId.ToString(),
                    ["slotId"] = req.SlotId.ToString(),
                    ["bidId"] = req.BidId.ToString(),
                },
            }, cancellationToken: ct);

            return CaptureOutcome.Ok(intent.Id);
        }
        catch (StripeException ex)
        {
            _log.LogWarning(ex, "Stripe capture failed for quest {QuestId}", req.QuestId);
            return CaptureOutcome.Fail(ex.StripeError?.Message ?? ex.Message);
        }
    }

    public async Task<PayoutOutcome> ReleaseAsync(EscrowPayment payment, CancellationToken ct = default)
    {
        try
        {
            var questerAccountId = await _db.Users
                .Where(u => u.Id == payment.QuesterId)
                .Select(u => u.StripeConnectAccountId)
                .FirstOrDefaultAsync(ct);

            if (string.IsNullOrWhiteSpace(questerAccountId))
                return PayoutOutcome.Fail("Quester has no connected Stripe account.");

            var transfer = await new TransferService().CreateAsync(new TransferCreateOptions
            {
                Amount = payment.QuesterPayoutCents,
                Currency = payment.Currency.ToLowerInvariant(),
                Destination = questerAccountId,
                Metadata = new Dictionary<string, string> { ["escrowId"] = payment.Id.ToString() },
            }, cancellationToken: ct);

            return PayoutOutcome.Ok(transfer.Id);
        }
        catch (StripeException ex)
        {
            _log.LogWarning(ex, "Stripe release failed for escrow {EscrowId}", payment.Id);
            return PayoutOutcome.Fail(ex.StripeError?.Message ?? ex.Message);
        }
    }

    public async Task<PayoutOutcome> RefundAsync(EscrowPayment payment, CancellationToken ct = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(payment.CaptureRef))
                return PayoutOutcome.Fail("No capture reference to refund.");

            var refund = await new RefundService().CreateAsync(new RefundCreateOptions
            {
                PaymentIntent = payment.CaptureRef,
            }, cancellationToken: ct);

            return PayoutOutcome.Ok(refund.Id);
        }
        catch (StripeException ex)
        {
            _log.LogWarning(ex, "Stripe refund failed for escrow {EscrowId}", payment.Id);
            return PayoutOutcome.Fail(ex.StripeError?.Message ?? ex.Message);
        }
    }
}
