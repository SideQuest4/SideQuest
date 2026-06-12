using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using SideQuest.Api.Data;
using SideQuest.Api.Dtos;
using SideQuest.Api.Hubs;

namespace SideQuest.Api.Services;

/// <summary>Pushes live quest/bid updates to connected clients over SignalR.</summary>
public interface IQuestNotifier
{
    /// <summary>Broadcast a quest's updated summary to all feed subscribers.</summary>
    Task QuestChangedAsync(Guid questId);

    /// <summary>
    /// Tell clients watching this quest that its bids changed, and refresh the
    /// feed summary too (bid count / slot fill may have moved).
    /// </summary>
    Task BidsChangedAsync(Guid questId);
}

public class QuestNotifier : IQuestNotifier
{
    private readonly IHubContext<QuestHub> _hub;
    private readonly AppDbContext _db;

    public QuestNotifier(IHubContext<QuestHub> hub, AppDbContext db)
    {
        _hub = hub;
        _db = db;
    }

    public async Task QuestChangedAsync(Guid questId)
    {
        var summary = await LoadSummaryAsync(questId);
        if (summary is not null)
            await _hub.Clients.All.SendAsync("QuestChanged", summary);
    }

    public async Task BidsChangedAsync(Guid questId)
    {
        await _hub.Clients.Group(QuestHub.GroupFor(questId))
            .SendAsync("BidsChanged", questId);
        await QuestChangedAsync(questId);
    }

    private async Task<QuestSummaryDto?> LoadSummaryAsync(Guid questId)
    {
        var quest = await _db.Quests
            .AsNoTracking()
            .Include(q => q.Category)
            .Include(q => q.Poster)
            .Include(q => q.Slots)
            .Include(q => q.Bids)
            .FirstOrDefaultAsync(q => q.Id == questId);

        return quest is null ? null : QuestSummaryDto.FromEntity(quest);
    }
}
