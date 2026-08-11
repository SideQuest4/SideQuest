using System.ComponentModel.DataAnnotations;
using SideQuest.Api.Models;

namespace SideQuest.Api.Dtos;

/// <summary>Payload for leaving a rating on a completed quest.</summary>
public record CreateRatingDto
{
    /// <summary>The user leaving the rating. Stubbed until auth is wired.</summary>
    [Required]
    public Guid RaterId { get; init; }

    /// <summary>The counterpart being rated.</summary>
    [Required]
    public Guid RateeId { get; init; }

    [Range(1, 5, ErrorMessage = "Stars must be between 1 and 5.")]
    public int Stars { get; init; }

    [MaxLength(1000)]
    public string? Comment { get; init; }
}

/// <summary>A rating left on a quest, with both sides' display names.</summary>
public record RatingDto(
    Guid Id,
    Guid QuestId,
    Guid RaterId,
    string RaterName,
    Guid RateeId,
    string RateeName,
    int Stars,
    string? Comment,
    DateTimeOffset CreatedAt)
{
    public static RatingDto FromEntity(Rating r) => new(
        r.Id, r.QuestId,
        r.RaterId, r.Rater?.DisplayName ?? "Unknown",
        r.RateeId, r.Ratee?.DisplayName ?? "Unknown",
        r.Stars, r.Comment, r.CreatedAt);
}

/// <summary>A user's received-rating aggregate plus their most recent reviews.</summary>
public record UserRatingSummaryDto(
    Guid UserId,
    double? AverageStars,
    int RatingCount,
    IReadOnlyList<RatingDto> Recent)
{
    public static UserRatingSummaryDto FromRatings(Guid userId, IReadOnlyList<Rating> received) =>
        new(
            userId,
            received.Count > 0 ? Math.Round(received.Average(r => r.Stars), 1) : null,
            received.Count,
            received
                .OrderByDescending(r => r.CreatedAt)
                .Take(10)
                .Select(RatingDto.FromEntity)
                .ToList());
}
