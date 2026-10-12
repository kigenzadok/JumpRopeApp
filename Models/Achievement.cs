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

    // Display Properties for Data Binding
    [Ignore]
    public string StatusText => IsUnlocked ? "Unlocked 🎉" : $"{Progress}/{Target}";

    [Ignore]
    public string StatusColor => IsUnlocked ? "#10B981" : "#64748B";

    [Ignore]
    public double ProgressRatio => Target > 0 ? Math.Min(1.0, (double)Progress / Target) : 0.0;
}