using SQLite;
using System;

namespace JumpRopeApp.Models;

public class Achievement
{
    [PrimaryKey]
    public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Icon { get; set; } = "🏆";
    public bool IsUnlocked { get; set; }
    public DateTime? UnlockedDate { get; set; }
    public int Progress { get; set; }
    public int Target { get; set; }
}