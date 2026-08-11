using System.ComponentModel.DataAnnotations;

namespace SideQuest.Api.Dtos;

/// <summary>Identifies the acting user for a slot action (stubbed until auth).</summary>
public record SlotActorDto
{
    [Required]
    public Guid UserId { get; init; }
}

/// <summary>Payload for opening a dispute on a single filled slot.</summary>
public record OpenSlotDisputeDto
{
    /// <summary>The participant raising the dispute (poster or the slot's quester).</summary>
    [Required]
    public Guid ByUserId { get; init; }

    [Required, MaxLength(1000)]
    public string Reason { get; init; } = string.Empty;
}

/// <summary>Payload for a poster reporting a quester no-show on a slot.</summary>
public record ReportNoShowDto
{
    /// <summary>The poster raising the report.</summary>
    [Required]
    public Guid ByUserId { get; init; }

    /// <summary>Optional note; a default reason is used when omitted.</summary>
    [MaxLength(1000)]
    public string? Reason { get; init; }
}

/// <summary>
/// Manual-review resolution of a slot dispute. "release" pays the slot's quester;
/// "refund" returns escrow to the poster, and then <see cref="SlotOutcome"/>
/// decides whether the slot reopens or is cancelled.
/// </summary>
public record ResolveSlotDisputeDto
{
    /// <summary>"release" or "refund".</summary>
    [Required]
    public string Outcome { get; init; } = string.Empty;

    /// <summary>For a refund: "reopen" (slot returns to the feed) or "cancel".</summary>
    public string? SlotOutcome { get; init; }
}
