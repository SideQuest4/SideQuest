namespace SideQuest.Api.Models;

/// <summary>Lifecycle of a quest (serialized as a string in the API).</summary>
public enum QuestStatus
{
    /// <summary>Accepting bids; no slots filled yet.</summary>
    Open = 0,

    /// <summary>Some slots filled, still accepting bids for the rest.</summary>
    Filling = 1,

    /// <summary>All slots filled; auto-closed and no longer accepting bids.</summary>
    Closed = 2,

    /// <summary>All work delivered and confirmed; payout released.</summary>
    Complete = 3,

    /// <summary>Flagged for manual review (founder-mediated dispute in V1).</summary>
    Disputed = 4,
}

/// <summary>State of a single quest slot.</summary>
public enum SlotStatus
{
    /// <summary>Empty and available to be filled by an accepted bid.</summary>
    Open = 0,

    /// <summary>Assigned to a quester and in progress.</summary>
    Active = 1,

    /// <summary>Work confirmed complete for this slot.</summary>
    Completed = 2,

    /// <summary>Quester voluntarily withdrew; the slot reopens.</summary>
    Dropped = 3,

    /// <summary>Quester was removed by the poster; the slot reopens.</summary>
    Kicked = 4,
}

/// <summary>State of a quester's bid on a quest.</summary>
public enum BidStatus
{
    /// <summary>Submitted by a quester, awaiting poster review.</summary>
    Pending = 0,

    /// <summary>Poster proposed a different amount; awaiting quester response.</summary>
    Countered = 1,

    /// <summary>Accepted — the quester now fills a slot.</summary>
    Accepted = 2,

    /// <summary>Declined by the poster, or withdrawn by the quester.</summary>
    Declined = 3,
}
