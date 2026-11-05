using JumpRopeApp.Models;
using SQLite;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JumpRopeApp;

public class DatabaseService
{
    private SQLiteAsyncConnection? _db;

    private async Task InitAsync()
    {
        if (_db != null) return;
        var dbPath = Path.Combine(FileSystem.AppDataDirectory, "jumprope.db3");
        _db = new SQLiteAsyncConnection(dbPath);
        await _db.CreateTableAsync<WorkoutRecord>();
        await _db.CreateTableAsync<UserProfile>();
        await _db.CreateTableAsync<Routine>();
        await _db.CreateTableAsync<RoutineStep>();
    }

    public async Task<WorkoutRecord> SaveWorkoutAsync(WorkoutRecord record)
    {
        await InitAsync();
        await _db!.InsertAsync(record);
        return record;
    }

    public async Task DeleteWorkoutAsync(WorkoutRecord record)
    {
        await InitAsync();
        await _db!.DeleteAsync(record);
    }

    public async Task<List<WorkoutRecord>> GetWorkoutsAsync()
    {
        await InitAsync();
        return await _db!.Table<WorkoutRecord>().OrderByDescending(w => w.Date).ToListAsync();
    }

    // Profile Methods
    public async Task<UserProfile> GetProfileAsync()
    {
        await InitAsync();
        var profile = await _db!.Table<UserProfile>().FirstOrDefaultAsync();
        if (profile == null)
        {
            profile = new UserProfile();
            await _db.InsertAsync(profile);
        }
        return profile;
    }

    public async Task SaveProfileAsync(UserProfile profile)
    {
        await InitAsync();
        await _db!.InsertOrReplaceAsync(profile);
    }

    public async Task SaveOrUpdateProfileAsync(UserProfile profile)
    {
        await SaveProfileAsync(profile);
    }

    // Lifetime Aggregation Helper
    public async Task<(int totalJumps, int totalSets, int totalWorkouts, double totalCalories)> GetLifetimeStatsAsync()
    {
        var workouts = await GetWorkoutsAsync();
        if (workouts.Count == 0) return (0, 0, 0, 0);

        int jumps = workouts.Sum(w => w.TotalJumps);
        int sets = workouts.Sum(w => w.SetsCompleted);
        int count = workouts.Count;
        double calories = workouts.Sum(w => w.CaloriesBurned);

        return (jumps, sets, count, calories);
    }

    // Daily Stats Helper
    public async Task<(int todaySets, int todayJumps)> GetDayStatsAsync(DateTime date)
    {
        var workouts = await GetWorkoutsAsync();
        var dayRecords = workouts.Where(w => w.Date.Date == date.Date).ToList();

        int sets = dayRecords.Sum(w => w.SetsCompleted);
        int jumps = dayRecords.Sum(w => w.TotalJumps);

        return (sets, jumps);
    }

    // Personal Records Helper
    public async Task<(int maxJumps, int maxSets, int streak)> GetPersonalRecordsAsync()
    {
        var workouts = await GetWorkoutsAsync();
        if (workouts.Count == 0) return (0, 0, 0);

        int maxJumps = workouts.Max(w => w.TotalJumps);
        int maxSets = workouts.Max(w => w.SetsCompleted);

        // Calculate current consecutive day streak
        int streak = 0;
        var uniqueDates = workouts.Select(w => w.Date.Date).Distinct().OrderByDescending(d => d).ToList();

        DateTime checkDate = DateTime.Today;
        if (!uniqueDates.Contains(checkDate))
        {
            checkDate = checkDate.AddDays(-1);
        }

        while (uniqueDates.Contains(checkDate))
        {
            streak++;
            checkDate = checkDate.AddDays(-1);
        }

        return (maxJumps, maxSets, streak);
    }

    // Weekly Totals Helper
    // Weekly Jump Totals Helper returning Day Name + Jumps tuple
    public async Task<List<(string DayName, int Jumps)>> GetWeeklyJumpTotalsAsync()
    {
        var workouts = await GetWorkoutsAsync();
        var weeklyTotals = new List<(string DayName, int Jumps)>();

        for (int i = 6; i >= 0; i--)
        {
            var targetDate = DateTime.Today.AddDays(-i);
            int jumps = workouts.Where(w => w.Date.Date == targetDate.Date).Sum(w => w.TotalJumps);
            string dayName = targetDate.ToString("ddd"); // "Mon", "Tue", etc.

            weeklyTotals.Add((dayName, jumps));
        }

        return weeklyTotals;
    }

    // CSV Export Helper
    public async Task<string> ExportWorkoutsToCsvAsync()
    {
        var workouts = await GetWorkoutsAsync();
        var sb = new StringBuilder();

        sb.AppendLine("Id,Date,WorkoutType,SetsCompleted,TotalJumps,DurationSeconds,CaloriesBurned");

        foreach (var w in workouts)
        {
            sb.AppendLine($"{w.Id},{w.Date:yyyy-MM-dd HH:mm:ss},\"{w.WorkoutType}\",{w.SetsCompleted},{w.TotalJumps},{w.DurationSeconds},{w.CaloriesBurned}");
        }

        string filePath = Path.Combine(FileSystem.CacheDirectory, "workout_history_export.csv");
        await File.WriteAllTextAsync(filePath, sb.ToString());

        return filePath;
    }

    public async Task<(int Level, int CurrentXp, int NextLevelXp, double Progress)> GetUserLevelAsync()
    {
        var workouts = await GetWorkoutsAsync();
        int totalJumps = workouts.Sum(w => w.TotalJumps);
        int totalCalories = (int)workouts.Sum(w => w.CaloriesBurned);

        // 1 Jump = 1 XP, 1 Calorie = 5 XP
        int totalXp = totalJumps + (totalCalories * 5);

        int level = 1;
        int xpForNext = 1000;
        int xpAccumulated = totalXp;

        while (xpAccumulated >= xpForNext)
        {
            xpAccumulated -= xpForNext;
            level++;
            xpForNext = (int)(xpForNext * 1.25);
        }

        double progress = (double)xpAccumulated / xpForNext;
        return (level, xpAccumulated, xpForNext, progress);
    }

    public async Task<List<Achievement>> GetAchievementsAsync()
    {
        var workouts = await GetWorkoutsAsync();
        var (maxJumps, maxSets, streak) = await GetPersonalRecordsAsync();

        int totalJumps = workouts.Sum(w => w.TotalJumps);
        int totalSets = workouts.Sum(w => w.SetsCompleted);
        int totalCalories = (int)workouts.Sum(w => w.CaloriesBurned);

        var badges = new List<Achievement>();

        // -------------------------------------------------------------
        // PART 1: MULTI-TIERED LIFETIME BADGES (Bronze -> Silver -> Gold -> Platinum)
        // -------------------------------------------------------------
        badges.Add(CreateTieredBadge(
            "century_club", "Century Club", "Complete {0} total sets", "💯",
            totalSets, new[] { 100, 500, 1000, 5000 }));

        badges.Add(CreateTieredBadge(
            "jump_master", "Jump Volume", "Accumulate {0:N0} total jumps", "⚡",
            totalJumps, new[] { 10000, 50000, 100000, 500000 }));

        badges.Add(CreateTieredBadge(
            "streak_master", "On Fire", "Reach a {0}-day daily streak", "🔥",
            streak, new[] { 7, 14, 30, 100 }));

        badges.Add(CreateTieredBadge(
            "calorie_burner", "Calorie Crusher", "Burn {0:N0} total calories", "🔥",
            totalCalories, new[] { 1000, 5000, 25000, 100000 }));

        // -------------------------------------------------------------
        // PART 2: DYNAMIC WEEKLY CHALLENGES (Resets every Monday)
        // -------------------------------------------------------------
        DateTime startOfWeek = DateTime.Today.AddDays(-(int)DateTime.Today.DayOfWeek + (int)DayOfWeek.Monday);
        var weeklyWorkouts = workouts.Where(w => w.Date.Date >= startOfWeek).ToList();

        int weeklyJumps = weeklyWorkouts.Sum(w => w.TotalJumps);
        int weeklySessions = weeklyWorkouts.Count;
        int weeklyActiveMinutes = weeklyWorkouts.Sum(w => w.DurationSeconds) / 60;

        badges.Add(new Achievement
        {
            Id = $"weekly_jumps_{startOfWeek:yyyyMMdd}",
            Title = "🎯 Weekly Target: 5,000 Jumps",
            Description = "Log 5,000 jumps during the current week.",
            Icon = "📅",
            Category = "Weekly Challenge",
            Target = 5000,
            Progress = Math.Min(weeklyJumps, 5000),
            IsUnlocked = weeklyJumps >= 5000
        });

        badges.Add(new Achievement
        {
            Id = $"weekly_consistency_{startOfWeek:yyyyMMdd}",
            Title = "⏱️ Weekly Consistency",
            Description = "Complete 3 workout sessions this week.",
            Icon = "🏋️",
            Category = "Weekly Challenge",
            Target = 3,
            Progress = Math.Min(weeklySessions, 3),
            IsUnlocked = weeklySessions >= 3
        });

        badges.Add(new Achievement
        {
            Id = $"weekly_endurance_{startOfWeek:yyyyMMdd}",
            Title = "⏳ Endurance Push",
            Description = "Log 30 minutes of active jumping time this week.",
            Icon = "⚡",
            Category = "Weekly Challenge",
            Target = 30,
            Progress = Math.Min(weeklyActiveMinutes, 30),
            IsUnlocked = weeklyActiveMinutes >= 30
        });

        return badges;
    }

    private Achievement CreateTieredBadge(
        string baseId, string title, string descriptionPattern, string icon,
        int currentValue, int[] tiers)
    {
        int currentTierIndex = 0;

        // Find current tier target
        while (currentTierIndex < tiers.Length && currentValue >= tiers[currentTierIndex])
        {
            currentTierIndex++;
        }

        bool isFullyMaxed = currentTierIndex >= tiers.Length;
        int target = isFullyMaxed ? tiers[^1] : tiers[currentTierIndex];

        string tierLabel = currentTierIndex switch
        {
            0 => "Bronze",
            1 => "Silver",
            2 => "Gold",
            3 => "Platinum",
            _ => "Master"
        };

        return new Achievement
        {
            Id = $"{baseId}_tier_{currentTierIndex}",
            Title = $"{title} ({tierLabel})",
            Description = string.Format(descriptionPattern, target),
            Icon = icon,
            Category = "Lifetime",
            Target = target,
            Progress = Math.Min(currentValue, target),
            IsUnlocked = currentValue >= target && isFullyMaxed
        };
    }
    public async Task SaveRoutineAsync(Routine routine, List<RoutineStep> steps)
    {
        await InitAsync();
        await _db!.InsertAsync(routine);

        foreach (var step in steps)
        {
            step.RoutineId = routine.Id;
            await _db.InsertAsync(step);
        }
    }

    public async Task<List<Routine>> GetRoutinesAsync()
    {
        await InitAsync();
        return await _db!.Table<Routine>().ToListAsync();
    }

    public async Task<List<RoutineStep>> GetRoutineStepsAsync(int routineId)
    {
        await InitAsync();
        return await _db!.Table<RoutineStep>()
                         .Where(s => s.RoutineId == routineId)
                         .OrderBy(s => s.StepOrder)
                         .ToListAsync();
    }
    public async Task SaveDailyStepsAsync(DateTime date, int steps)
    {
        await InitAsync();

        // Store daily step count in SQLite
        var existingRecord = await _db!.Table<WorkoutRecord>()
                                      .FirstOrDefaultAsync(w => w.Date.Date == date.Date);

        if (existingRecord != null)
        {
            existingRecord.TotalSteps = steps;
            await _db.UpdateAsync(existingRecord);
        }
    }

}