using JumpRopeApp.Models;
using SQLite;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace JumpRopeApp;

public class DatabaseService
{
    private SQLiteAsyncConnection? _database;

    private async Task InitAsync()
    {
        if (_database is not null)
            return;

        var dbPath = Path.Combine(FileSystem.AppDataDirectory, "jumprope.db3");
        _database = new SQLiteAsyncConnection(dbPath);

        // Ensure the table exists before any queries run
        await _database.CreateTableAsync<WorkoutRecord>();
    }

    public async Task<(int Sets, int Jumps)> GetDayStatsAsync(DateTime date)
    {
        await InitAsync();
        if (_database == null) return (0, 0);

        DateTime startOfDay = date.Date;
        DateTime endOfDay = startOfDay.AddDays(1);

        var records = await _database.Table<WorkoutRecord>()
                                     .Where(w => w.Date >= startOfDay && w.Date < endOfDay)
                                     .ToListAsync();

        int totalSets = records?.Sum(r => r.SetsCompleted) ?? 0;
        int totalJumps = records?.Sum(r => r.TotalJumps) ?? 0;

        return (totalSets, totalJumps);
    }

    public async Task<int> GetCurrentStreakAsync()
    {
        await InitAsync();
        if (_database == null) return 0;

        var allWorkouts = await _database.Table<WorkoutRecord>().ToListAsync();
        if (allWorkouts == null || !allWorkouts.Any()) return 0;

        var workoutDates = allWorkouts.Select(w => w.Date.Date).Distinct().OrderByDescending(d => d).ToList();

        int streak = 0;
        DateTime checkDate = DateTime.Now.Date;

        if (!workoutDates.Contains(checkDate))
        {
            checkDate = checkDate.AddDays(-1);
        }

        while (workoutDates.Contains(checkDate))
        {
            streak++;
            checkDate = checkDate.AddDays(-1);
        }

        return streak;
    }

    public async Task<(int Jumps, int Sets, int Workouts)> GetLifetimeStatsAsync()
    {
        await InitAsync();
        if (_database == null) return (0, 0, 0);

        var records = await _database.Table<WorkoutRecord>().ToListAsync();
        if (records == null || !records.Any()) return (0, 0, 0);

        int jumps = records.Sum(r => r.TotalJumps);
        int sets = records.Sum(r => r.SetsCompleted);
        int workouts = records.Count;

        return (jumps, sets, workouts);
    }

    public async Task SaveWorkoutAsync(WorkoutRecord record)
    {
        await InitAsync();
        if (_database != null)
        {
            await _database.InsertAsync(record);
        }

    }
    public async Task SaveWorkoutAsync(int sets, int jumps, int jumpSecs = 0, int restSecs = 0)
    {
        var record = new WorkoutRecord
        {
            Date = DateTime.Now,
            SetsCompleted = sets,
            TotalJumps = jumps,
            JumpSecs = jumpSecs,
            RestSecs = restSecs
        };

        await SaveWorkoutAsync(record);
    }
}