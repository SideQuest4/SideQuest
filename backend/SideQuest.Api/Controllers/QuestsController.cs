using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SideQuest.Api.Data;
using SideQuest.Api.Dtos;
using SideQuest.Api.Models;

namespace SideQuest.Api.Controllers;

[ApiController]
[Route("api/quests")]
public class QuestsController : ControllerBase
{
    private readonly AppDbContext _db;

    public QuestsController(AppDbContext db) => _db = db;

    /// <summary>
    /// Quest feed. Returns open quests by default, newest first, with optional
    /// category, search, and status filters plus simple paging.
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<QuestSummaryDto>>> GetFeed(
        [FromQuery] string? category,
        [FromQuery] string? search,
        [FromQuery] string status = "open",
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, 50);

        var query = _db.Quests
            .Include(q => q.Category)
            .Include(q => q.Poster)
            .Include(q => q.Slots)
            .Include(q => q.Bids)
            .AsQueryable();

        // Status filter: "open" (default), "all", or a specific QuestStatus.
        if (!string.Equals(status, "all", StringComparison.OrdinalIgnoreCase))
        {
            if (Enum.TryParse<QuestStatus>(status, ignoreCase: true, out var parsed))
                query = query.Where(q => q.Status == parsed);
            else
                return BadRequest($"Unknown status '{status}'.");
        }

        if (!string.IsNullOrWhiteSpace(category))
            query = query.Where(q => q.Category!.Slug == category);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(q =>
                EF.Functions.Like(q.Title, $"%{term}%") ||
                EF.Functions.Like(q.Description, $"%{term}%"));
        }

        var results = await query
            .OrderByDescending(q => q.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return Ok(results.Select(QuestSummaryDto.FromEntity));
    }

    /// <summary>Full detail for a single quest, including its slots.</summary>
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<QuestDetailDto>> GetById(Guid id)
    {
        var quest = await _db.Quests
            .Include(q => q.Category)
            .Include(q => q.Poster)
            .Include(q => q.Slots)
            .Include(q => q.Bids)
            .FirstOrDefaultAsync(q => q.Id == id);

        return quest is null ? NotFound() : Ok(QuestDetailDto.FromEntity(quest));
    }

    /// <summary>
    /// Create a quest with the requested number of open slots.
    /// </summary>
    /// <remarks>
    /// Auth is not wired yet: the poster defaults to the first seeded user.
    /// Once Auth0 is integrated this will resolve the poster from the access token.
    /// </remarks>
    [HttpPost]
    public async Task<ActionResult<QuestDetailDto>> Create([FromBody] CreateQuestDto dto)
    {
        var category = await _db.Categories.FindAsync(dto.CategoryId);
        if (category is null)
            return BadRequest($"Category '{dto.CategoryId}' does not exist.");

        // TODO(auth): resolve poster from the authenticated Auth0 user.
        var poster = await _db.Users.OrderBy(u => u.CreatedAt).FirstOrDefaultAsync();
        if (poster is null)
            return Problem("No users exist to attribute this quest to.", statusCode: 500);

        var quest = new Quest
        {
            Title = dto.Title.Trim(),
            Description = dto.Description.Trim(),
            BudgetCents = dto.BudgetCents,
            Currency = dto.Currency.ToUpperInvariant(),
            Location = string.IsNullOrWhiteSpace(dto.Location) ? null : dto.Location.Trim(),
            Deadline = dto.Deadline,
            CategoryId = category.Id,
            PosterId = poster.Id,
            Status = QuestStatus.Open,
        };
        for (var i = 0; i < dto.SlotCount; i++)
            quest.Slots.Add(new QuestSlot { Status = SlotStatus.Open });

        _db.Quests.Add(quest);
        await _db.SaveChangesAsync();

        // Reload with navigation properties for the response DTO.
        await _db.Entry(quest).Reference(q => q.Category).LoadAsync();
        await _db.Entry(quest).Reference(q => q.Poster).LoadAsync();

        return CreatedAtAction(nameof(GetById), new { id = quest.Id },
            QuestDetailDto.FromEntity(quest));
    }
}
