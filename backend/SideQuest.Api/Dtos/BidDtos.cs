using System.ComponentModel.DataAnnotations;
using SideQuest.Api.Models;

namespace SideQuest.Api.Dtos;

/// <summary>A bid as returned to clients.</summary>
public record BidDto(
    Guid Id,
    Guid QuestId,
    QuesterDto Quester,
    long AmountCents,
    long? CounterAmountCents,
    long EffectiveAmountCents,
    string? Message,
    string Status,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt)
{
    public static BidDto FromEntity(Bid b) => new(
        b.Id, b.QuestId, QuesterDto.FromEntity(b.Quester!),
        b.AmountCents, b.CounterAmountCents, b.EffectiveAmountCents,
        b.Message, b.Status.ToString(), b.CreatedAt, b.UpdatedAt);
}

public record QuesterDto(Guid Id, string DisplayName, string? AvatarUrl)
{
    public static QuesterDto FromEntity(User u) => new(u.Id, u.DisplayName, u.AvatarUrl);
}

/// <summary>Payload for a quester submitting a bid.</summary>
public record CreateBidDto
{
    [Range(100, 100_000_00, ErrorMessage = "Bid must be between $1 and $100,000.")]
    public long AmountCents { get; init; }

    [MaxLength(1000)]
    public string? Message { get; init; }
}

/// <summary>Payload for a poster countering a bid.</summary>
public record CounterBidDto
{
    [Range(100, 100_000_00, ErrorMessage = "Counter must be between $1 and $100,000.")]
    public long CounterAmountCents { get; init; }
}

/// <summary>Payload for a quester responding to a poster's counter-offer.</summary>
public record RespondCounterDto
{
    /// <summary>True to accept the counter (fills a slot), false to decline.</summary>
    public bool Accept { get; init; }
}
