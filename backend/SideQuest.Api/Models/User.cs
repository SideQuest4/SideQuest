using System.ComponentModel.DataAnnotations;

namespace SideQuest.Api.Models;

/// <summary>
/// A platform participant. The same user can act as a poster (creating quests)
/// and a quester (bidding on quests).
/// </summary>
public class User
{
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>Auth0 subject identifier (e.g. "auth0|abc123"). Unique per user.</summary>
    [MaxLength(128)]
    public string? Auth0Id { get; set; }

    [Required, MaxLength(80)]
    public string DisplayName { get; set; } = string.Empty;

    [Required, EmailAddress, MaxLength(256)]
    public string Email { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? AvatarUrl { get; set; }

    /// <summary>City-level location label, e.g. "Seattle, WA".</summary>
    [MaxLength(120)]
    public string? Location { get; set; }

    [MaxLength(1000)]
    public string? Bio { get; set; }

    /// <summary>Stripe Connect account id, set once the user onboards to receive payouts.</summary>
    [MaxLength(64)]
    public string? StripeConnectAccountId { get; set; }

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    // Navigation
    public ICollection<Quest> PostedQuests { get; set; } = new List<Quest>();
    public ICollection<Bid> Bids { get; set; } = new List<Bid>();
    public ICollection<Rating> RatingsGiven { get; set; } = new List<Rating>();
    public ICollection<Rating> RatingsReceived { get; set; } = new List<Rating>();
}
