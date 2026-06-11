using System.ComponentModel.DataAnnotations;

namespace SideQuest.Api.Models;

/// <summary>A quest category used for browsing and filtering the feed.</summary>
public class Category
{
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required, MaxLength(60)]
    public string Name { get; set; } = string.Empty;

    /// <summary>URL-friendly identifier, e.g. "home-repair".</summary>
    [Required, MaxLength(60)]
    public string Slug { get; set; } = string.Empty;

    public ICollection<Quest> Quests { get; set; } = new List<Quest>();
}
