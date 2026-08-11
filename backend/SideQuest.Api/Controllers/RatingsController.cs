using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SideQuest.Api.Data;
using SideQuest.Api.Dtos;
using SideQuest.Api.Models;

namespace SideQuest.Api.Controllers;

[ApiController]
[Route("api")]
public class RatingsController : ControllerBase
{
    private readonly AppDbContext _db;

    public RatingsController(AppDbContext db)
    {
        _db = db;
    }

    /// <summary>All ratings left on a quest, both directions.</summary>
    [HttpGet("quests/{questId:guid}/ratings")]
    public async Task<ActionResult<IEnumerable<RatingDto>>> GetForQuest(Guid questId)
    {
        var ratings = await _db.Ratings
            .Where(r => r.QuestId == questId)
            .Include(r => r.Rater)
            .Include(r => r.Ratee)
            .OrderBy(r => r.CreatedAt)
            .ToListAsync();

        return Ok(ratings.Select(RatingDto.FromEntity));
    }

    /// <summary>A user's received ratings with their average and count.</summary>
    [HttpGet("users/{userId:guid}/ratings")]
    public async Task<ActionResult<UserRatingSummaryDto>> GetForUser(Guid userId)
    {
        var received = await _db.Ratings
            .Where(r => r.RateeId == userId)
            .Include(r => r.Rater)
            .Include(r => r.Ratee)
            .ToListAsync();

        return Ok(UserRatingSummaryDto.FromRatings(userId, received));
    }

    /// <summary>
    /// Leave a rating on a completed quest. Only the two sides who actually
    /// worked together may rate each other, once per direction.
    /// </summary>
    /// <remarks>
    /// Auth is stubbed, so the rater is taken from the request body. Once Auth0
    /// is wired the rater will be resolved from the access token instead.
    /// </remarks>
    [HttpPost("quests/{questId:guid}/ratings")]
    public async Task<ActionResult<RatingDto>> Create(Guid questId, [FromBody] CreateRatingDto dto)
    {
        if (dto.RaterId == dto.RateeId)
            return BadRequest("You can't rate yourself.");

        var quest = await _db.Quests
            .Include(q => q.Slots)
            .FirstOrDefaultAsync(q => q.Id == questId);

        if (quest is null) return NotFound("Quest not found.");
        if (quest.Status != QuestStatus.Complete)
            return Conflict("Ratings can only be left once the quest is complete.");

        // Participants: the poster and every quester who filled a slot.
        var questerIds = quest.Slots
            .Where(s => s.AssignedQuesterId is not null)
            .Select(s => s.AssignedQuesterId!.Value)
            .ToHashSet();

        // A legal rating is poster -> quester or quester -> poster.
        var posterRatesQuester = dto.RaterId == quest.PosterId && questerIds.Contains(dto.RateeId);
        var questerRatesPoster = dto.RateeId == quest.PosterId && questerIds.Contains(dto.RaterId);
        if (!posterRatesQuester && !questerRatesPoster)
            return BadRequest("Both users must have taken part in this quest.");

        var duplicate = await _db.Ratings.AnyAsync(r =>
            r.QuestId == questId && r.RaterId == dto.RaterId && r.RateeId == dto.RateeId);
        if (duplicate)
            return Conflict("You've already rated this person for this quest.");

        var rater = await _db.Users.FindAsync(dto.RaterId);
        var ratee = await _db.Users.FindAsync(dto.RateeId);
        if (rater is null || ratee is null)
            return BadRequest("Rater or ratee does not exist.");

        var rating = new Rating
        {
            QuestId = questId,
            RaterId = dto.RaterId,
            RateeId = dto.RateeId,
            Stars = dto.Stars,
            Comment = string.IsNullOrWhiteSpace(dto.Comment) ? null : dto.Comment.Trim(),
        };

        _db.Ratings.Add(rating);
        await _db.SaveChangesAsync();

        rating.Rater = rater;
        rating.Ratee = ratee;
        return CreatedAtAction(nameof(GetForQuest), new { questId }, RatingDto.FromEntity(rating));
    }
}
