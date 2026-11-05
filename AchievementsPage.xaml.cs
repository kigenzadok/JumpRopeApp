using JumpRopeApp.Models;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace JumpRopeApp;

public class BadgeItem
{
    public string Icon { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public double ProgressRatio { get; set; }
    public string StatusText { get; set; } = string.Empty;
    public Color StatusColor { get; set; } = Colors.Gray;
    public Color ProgressColor { get; set; } = Color.FromArgb("#6366F1");
}

public partial class AchievementsPage : ContentPage
{
    private readonly DatabaseService _dbService = new();

    public AchievementsPage()
    {
        InitializeComponent();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadAchievementsAsync();
    }

    private async Task LoadAchievementsAsync()
    {
        var workouts = await _dbService.GetWorkoutsAsync() ?? new List<WorkoutRecord>();
        int currentDailySteps = Preferences.Default.Get("SavedDailySteps", 0);
        int accumulativeSteps = Preferences.Default.Get("SavedAccumulativeSteps", 0);

        int totalJumps = workouts.Sum(w => w.TotalJumps);
        int totalSets = workouts.Sum(w => w.DurationSeconds / 30); // Approximate sets or property
        int maxJumpsInSession = workouts.Any() ? workouts.Max(w => w.TotalJumps) : 0;
        int maxStepsInDay = workouts.Any() ? Math.Max(workouts.Max(w => w.DailySteps), currentDailySteps) : currentDailySteps;

        // --- Calculate Level & XP ---
        // 1 Jump = 1 XP, 10 Steps = 1 XP
        int totalXp = totalJumps + (accumulativeSteps / 10);
        int level = (totalXp / 1000) + 1;
        int currentLevelXp = totalXp % 1000;

        LevelNumberLabel.Text = $"Level {level}";
        XpRatioLabel.Text = $"{currentLevelXp:N0} / 1,000 XP";
        XpProgressBar.Progress = Math.Min(1.0, currentLevelXp / 1000.0);

        // --- Build Badges List ---
        var badges = new List<BadgeItem>
        {
            // 1. First Workout
            CreateBadge("👟", "First Jump", "Complete your first workout session.", workouts.Count, 1, "#6366F1"),

            // 2. Step Badge: Daily Walker
            CreateBadge("🚶‍♂️", "Daily Walker", "Reach 10,000 steps in a single day.", maxStepsInDay, 10000, "#10B981"),

            // 3. Century Club
            CreateBadge("💯", "Century Club", "Complete 100 total sets across all workouts.", totalSets, 100, "#6366F1"),

            // 4. Lifetime Steps: Marathoner
            CreateBadge("🏃‍♂️", "Step Marathoner", "Accumulate 50,000 lifetime steps.", accumulativeSteps, 50000, "#10B981"),

            // 5. 10k Jumper
            CreateBadge("⚡", "10k Jumper", "Accumulate 10,000 lifetime jumps.", totalJumps, 10000, "#6366F1"),

            // 6. Iron Legs
            CreateBadge("🏋️‍♂️", "Iron Legs", "Complete a single session with over 1,000 jumps.", maxJumpsInSession, 1000, "#F59E0B")
        };

        BadgesCollectionView.ItemsSource = badges;
    }

    private BadgeItem CreateBadge(string icon, string title, string description, int current, int target, string hexColor)
    {
        double ratio = Math.Min(1.0, (double)current / target);
        bool isUnlocked = current >= target;

        return new BadgeItem
        {
            Icon = icon,
            Title = title,
            Description = description,
            ProgressRatio = ratio,
            StatusText = isUnlocked ? "Unlocked 🎉" : $"{current:N0}/{target:N0}",
            StatusColor = isUnlocked ? Color.FromArgb("#10B981") : Color.FromArgb("#334155"),
            ProgressColor = Color.FromArgb(hexColor)
        };
    }
}