using System.ComponentModel.DataAnnotations;

namespace SideQuest.Api.Dtos;

/// <summary>Payload for opening a dispute on an in-progress quest.</summary>
public record OpenDisputeDto
{
    /// <summary>The participant raising the dispute. Stubbed until auth is wired.</summary>
    [Required]
    public Guid ByUserId { get; init; }

    [Required, MaxLength(1000)]
    public string Reason { get; init; } = string.Empty;
}

/// <summary>Payload for a manual-review resolution of a disputed quest.</summary>
public record ResolveDisputeDto
{
    /// <summary>"refund" (return escrow to poster) or "release" (pay the questers).</summary>
    [Required]
    public string Outcome { get; init; } = string.Empty;
}
