using System.ComponentModel.DataAnnotations;

namespace SideQuest.Api.Models;

/// <summary>
/// A 1–5 star review left after a quest completes. Both poster and quester can
/// rate each other, so ratings are directional (<see cref="RaterId"/> →
/// <see cref="RateeId"/>).
/// </summary>
public class Rating
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid QuestId { get; set; }
    public Quest? Quest { get; set; }

    /// <summary>The user leaving the rating.</summary>
    public Guid RaterId { get; set; }
    public User? Rater { get; set; }

    /// <summary>The user being rated.</summary>
    public Guid RateeId { get; set; }
    public User? Ratee { get; set; }

    /// <summary>Score from 1 to 5.</summary>
    [Range(1, 5)]
    public int Stars { get; set; }

    [MaxLength(1000)]
    public string? Comment { get; set; }

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
