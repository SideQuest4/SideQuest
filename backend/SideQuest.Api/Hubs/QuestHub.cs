using Microsoft.AspNetCore.SignalR;

namespace SideQuest.Api.Hubs;

/// <summary>
/// Real-time channel for the live quest board.
///
/// Two streams of events go out to clients:
/// - <c>QuestChanged</c> — broadcast to everyone with an updated quest summary so
///   feeds reflect slot fills, status changes, and new quests live.
/// - <c>BidsChanged</c> — sent to the group watching a specific quest so its detail
///   view refreshes bids. Clients join/leave that group via the hub methods below.
/// </summary>
public class QuestHub : Hub
{
    public static string GroupFor(Guid questId) => $"quest-{questId}";

    /// <summary>Subscribe this connection to a quest's bid-level updates.</summary>
    public Task JoinQuest(Guid questId) =>
        Groups.AddToGroupAsync(Context.ConnectionId, GroupFor(questId));

    /// <summary>Unsubscribe this connection from a quest's updates.</summary>
    public Task LeaveQuest(Guid questId) =>
        Groups.RemoveFromGroupAsync(Context.ConnectionId, GroupFor(questId));
}
