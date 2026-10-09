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
        int totalWorkouts = workouts.Count;

        var badges = new List<Achievement>
        {
            new Achievement
            {
                Id = "first_step",
                Title = "First Jump",
                Description = "Complete your first workout session.",
                Icon = "👟",
                Target = 1,
                Progress = Math.Min(totalWorkouts, 1),
                IsUnlocked = totalWorkouts >= 1
            },
            new Achievement
            {
                Id = "century_club",
                Title = "Century Club",
                Description = "Complete 100 total sets across all workouts.",
                Icon = "💯",
                Target = 100,
                Progress = Math.Min(workouts.Sum(w => w.SetsCompleted), 100),
                IsUnlocked = workouts.Sum(w => w.SetsCompleted) >= 100
            },
            new Achievement
            {
                Id = "streak_master",
                Title = "On Fire",
                Description = "Reach a 7-day daily jump streak.",
                Icon = "🔥",
                Target = 7,
                Progress = Math.Min(streak, 7),
                IsUnlocked = streak >= 7
            },
            new Achievement
            {
                Id = "jump_master",
                Title = "10k Jumper",
                Description = "Accumulate 10,000 lifetime jumps.",
                Icon = "⚡",
                Target = 10000,
                Progress = Math.Min(totalJumps, 10000),
                IsUnlocked = totalJumps >= 10000
            },
            new Achievement
            {
                Id = "iron_legs",
                Title = "Iron Legs",
                Description = "Complete a single session with over 1,000 jumps.",
                Icon = "🏋️",
                Target = 1000,
                Progress = Math.Min(maxJumps, 1000),
                IsUnlocked = maxJumps >= 1000
            }
        };

        return badges;
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

}