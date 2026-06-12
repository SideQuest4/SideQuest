using Microsoft.EntityFrameworkCore;
using SideQuest.Api.Models;

namespace SideQuest.Api.Data;

/// <summary>
/// Seeds a small set of categories, users, and quests so the feed is populated
/// out of the box during local development. Safe to call on every startup — it
/// no-ops if data already exists.
/// </summary>
public static class SeedData
{
    public static async Task EnsureSeededAsync(AppDbContext db)
    {
        if (await db.Categories.AnyAsync())
            return;

        var categories = new[]
        {
            new Category { Name = "Physical", Slug = "physical" },
            new Category { Name = "Tech", Slug = "tech" },
            new Category { Name = "Creative", Slug = "creative" },
            new Category { Name = "Errands", Slug = "errands" },
            new Category { Name = "Knowledge", Slug = "knowledge" },
        };
        db.Categories.AddRange(categories);

        var alice = new User
        {
            DisplayName = "Alice Poster",
            Email = "alice@example.com",
            Location = "Seattle, WA",
            Bio = "Busy parent who outsources weekend tasks.",
        };
        // Several questers so multi-slot quests can be filled by different people
        // while auth is still stubbed (the bid stub rotates through available
        // questers — see BidsController.Submit).
        var bob = new User
        {
            DisplayName = "Bob Quester",
            Email = "bob@example.com",
            Location = "Seattle, WA",
            Bio = "Handyman and all-round fixer.",
        };
        var cara = new User
        {
            DisplayName = "Cara Quester",
            Email = "cara@example.com",
            Location = "Tacoma, WA",
            Bio = "Designer and part-time mover.",
        };
        var dan = new User
        {
            DisplayName = "Dan Quester",
            Email = "dan@example.com",
            Location = "Bellevue, WA",
            Bio = "CS student who tutors and fixes laptops.",
        };
        db.Users.AddRange(alice, bob, cara, dan);

        Category Cat(string slug) => categories.First(c => c.Slug == slug);

        var quests = new[]
        {
            NewQuest(alice, Cat("physical"),
                "Mount a 55\" TV on drywall",
                "Need a TV mounted on a drywall wall with the cables hidden in a cord cover. I have the mount and bracket.",
                budgetCents: 8000, location: "Seattle, WA", slots: 1),

            NewQuest(alice, Cat("physical"),
                "Help move a 1-bedroom apartment",
                "Two movers needed for ~3 hours on Saturday. Mostly boxes plus a couch and a bed frame. Truck is already rented.",
                budgetCents: 12000, location: "Seattle, WA", slots: 2),

            NewQuest(alice, Cat("tech"),
                "Set up a home mesh Wi-Fi system",
                "Replace my old router with a 3-node mesh kit and get good coverage across two floors.",
                budgetCents: 6000, location: "Remote OK for advice", slots: 1),

            NewQuest(alice, Cat("creative"),
                "Design a simple logo for a coffee cart",
                "Looking for a clean, friendly logo. Provide source files (SVG) and a couple of color variations.",
                budgetCents: 15000, location: "Remote", slots: 1),

            NewQuest(alice, Cat("knowledge"),
                "Tutor me on intro calculus (2 sessions)",
                "Need help understanding limits and derivatives before a midterm. Two 1-hour video sessions.",
                budgetCents: 9000, location: "Remote", slots: 1),
        };
        db.Quests.AddRange(quests);

        await db.SaveChangesAsync();
    }

    private static Quest NewQuest(
        User poster, Category category, string title, string description,
        long budgetCents, string location, int slots)
    {
        var quest = new Quest
        {
            Poster = poster,
            Category = category,
            Title = title,
            Description = description,
            BudgetCents = budgetCents,
            Location = location,
            Deadline = null,
            Status = QuestStatus.Open,
        };
        for (var i = 0; i < slots; i++)
            quest.Slots.Add(new QuestSlot { Status = SlotStatus.Open });
        return quest;
    }
}
