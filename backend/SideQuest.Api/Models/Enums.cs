namespace SideQuest.Api.Models;

/// <summary>Lifecycle of a quest.</summary>
public enum QuestStatus
{
    /// <summary>Visible in the feed and accepting bids.</summary>
    Open = 0,

    /// <summary>All slots filled; no longer accepting bids.</summary>
    Filled = 1,

    /// <summary>Work delivered and confirmed; payout released.</summary>
    Completed = 2,

    /// <summary>Cancelled by the poster before completion.</summary>
    Cancelled = 3,
}

/// <summary>State of a single quester's bid on a quest.</summary>
public enum BidStatus
{
    /// <summary>Submitted by a quester, awaiting poster review.</summary>
    Pending = 0,

    /// <summary>Poster proposed a different amount; awaiting quester response.</summary>
    Countered = 1,

    /// <summary>Accepted — the quester now fills a slot.</summary>
    Accepted = 2,

    /// <summary>Declined by the poster, or withdrawn by the quester.</summary>
    Rejected = 3,
}

/// <summary>State of a quest slot.</summary>
public enum SlotStatus
{
    /// <summary>Open for an accepted bid to fill.</summary>
    Open = 0,

    /// <summary>Assigned to a quester via an accepted bid.</summary>
    Filled = 1,

    /// <summary>Work confirmed complete for this slot.</summary>
    Completed = 2,
}
