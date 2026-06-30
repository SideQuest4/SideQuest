using System.ComponentModel.DataAnnotations;
using SideQuest.Api.Models;

namespace SideQuest.Api.Dtos;

/// <summary>Lightweight quest representation for feed listings.</summary>
public record QuestSummaryDto(
    Guid Id,
    string Title,
    string Description,
    long BudgetCents,
    string Currency,
    string? Location,
    DateTimeOffset? Deadline,
    string Status,
    int SlotCount,
    int OpenSlotCount,
    CategoryDto Category,
    PosterDto Poster,
    int BidCount,
    DateTimeOffset CreatedAt)
{
    public static QuestSummaryDto FromEntity(Quest q) => new(
        q.Id, q.Title, q.Description, q.BudgetCents, q.Currency, q.Location,
        q.Deadline, q.Status.ToString(), q.Slots.Count,
        q.Slots.Count(s => s.Status == SlotStatus.Open),
        CategoryDto.FromEntity(q.Category!), PosterDto.FromEntity(q.Poster!),
        q.Bids.Count, q.CreatedAt);
}

/// <summary>Full quest detail including slots.</summary>
public record QuestDetailDto(
    Guid Id,
    string Title,
    string Description,
    long BudgetCents,
    string Currency,
    string? Location,
    DateTimeOffset? Deadline,
    string Status,
    CategoryDto Category,
    PosterDto Poster,
    IReadOnlyList<SlotDto> Slots,
    int BidCount,
    EscrowSummaryDto Escrow,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt)
{
    public static QuestDetailDto FromEntity(Quest q) => new(
        q.Id, q.Title, q.Description, q.BudgetCents, q.Currency, q.Location,
        q.Deadline, q.Status.ToString(),
        CategoryDto.FromEntity(q.Category!), PosterDto.FromEntity(q.Poster!),
        q.Slots.OrderBy(s => s.CreatedAt).Select(SlotDto.FromEntity).ToList(),
        q.Bids.Count, EscrowSummaryDto.FromQuest(q), q.CreatedAt, q.UpdatedAt);
}

/// <summary>Aggregate escrow state for a quest, for display.</summary>
public record EscrowSummaryDto(
    int HeldCount,
    int ReleasedCount,
    long HeldAmountCents,
    long ReleasedAmountCents)
{
    public static EscrowSummaryDto FromQuest(Quest q)
    {
        var held = q.EscrowPayments.Where(p => p.Status == EscrowStatus.Held).ToList();
        var released = q.EscrowPayments.Where(p => p.Status == EscrowStatus.Released).ToList();
        return new EscrowSummaryDto(
            held.Count,
            released.Count,
            held.Sum(p => p.AmountCents),
            released.Sum(p => p.QuesterPayoutCents));
    }
}

public record SlotDto(Guid Id, string Status, Guid? AssignedQuesterId)
{
    public static SlotDto FromEntity(QuestSlot s) =>
        new(s.Id, s.Status.ToString(), s.AssignedQuesterId);
}

public record CategoryDto(Guid Id, string Name, string Slug)
{
    public static CategoryDto FromEntity(Category c) => new(c.Id, c.Name, c.Slug);
}

public record PosterDto(Guid Id, string DisplayName, string? AvatarUrl)
{
    public static PosterDto FromEntity(User u) => new(u.Id, u.DisplayName, u.AvatarUrl);
}

/// <summary>Payload for creating a quest.</summary>
public record CreateQuestDto
{
    [Required, MaxLength(120)]
    public string Title { get; init; } = string.Empty;

    [Required, MaxLength(4000)]
    public string Description { get; init; } = string.Empty;

    [Range(100, 100_000_00, ErrorMessage = "Budget must be between $1 and $100,000.")]
    public long BudgetCents { get; init; }

    [MaxLength(3)]
    public string Currency { get; init; } = "USD";

    [MaxLength(160)]
    public string? Location { get; init; }

    public DateTimeOffset? Deadline { get; init; }

    [Required]
    public Guid CategoryId { get; init; }

    /// <summary>Number of identical slots to offer (1–20).</summary>
    [Range(1, 20)]
    public int SlotCount { get; init; } = 1;
}
