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
        if (_database is not null) return;

        var dbPath = Path.Combine(FileSystem.AppDataDirectory, "jumprope.db3");
        _database = new SQLiteAsyncConnection(dbPath);

        await _database.CreateTableAsync<WorkoutRecord>();
        await _database.CreateTableAsync<UserProfile>();
    }

    // ==========================================
    // 1. CALENDAR & WORKOUT LOG QUERIES
    // ==========================================

    public async Task<List<WorkoutRecord>> GetWorkoutsForDateAsync(DateTime date)
    {
        await InitAsync();
        if (_database == null) return new List<WorkoutRecord>();

        DateTime startOfDay = date.Date;
        DateTime endOfDay = startOfDay.AddDays(1);

        return await _database.Table<WorkoutRecord>()
                              .Where(w => w.Date >= startOfDay && w.Date < endOfDay)
                              .OrderByDescending(w => w.Date)
                              .ToListAsync();
    }

    public async Task<(int Sets, int Jumps)> GetDayStatsAsync(DateTime date)
    {
        var records = await GetWorkoutsForDateAsync(date);
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

        if (!workoutDates.Contains(checkDate)) checkDate = checkDate.AddDays(-1);

        while (workoutDates.Contains(checkDate))
        {
            streak++;
            checkDate = checkDate.AddDays(-1);
        }

        return streak;
    }

    public async Task SaveWorkoutAsync(WorkoutRecord record)
    {
        await InitAsync();
        if (_database != null) await _database.InsertAsync(record);
    }

    public async Task ClearAllWorkoutsAsync()
    {
        await InitAsync();
        if (_database != null)
        {
            await _database.DeleteAllAsync<WorkoutRecord>();
        }
    }

    // ==========================================
    // 2. LIFETIME ANALYTICS
    // ==========================================

    public async Task<(int TotalJumps, int TotalSets, int TotalWorkouts)> GetLifetimeStatsAsync()
    {
        await InitAsync();
        if (_database == null) return (0, 0, 0);

        var allRecords = await _database.Table<WorkoutRecord>().ToListAsync();

        int totalJumps = allRecords?.Sum(r => r.TotalJumps) ?? 0;
        int totalSets = allRecords?.Sum(r => r.SetsCompleted) ?? 0;
        int totalWorkouts = allRecords?.Count ?? 0;

        return (totalJumps, totalSets, totalWorkouts);
    }

    // ==========================================
    // 3. USER PROFILE CRUD OPERATIONS
    // ==========================================

    public async Task<UserProfile?> GetProfileAsync()
    {
        await InitAsync();
        if (_database == null) return null;
        return await _database.Table<UserProfile>().FirstOrDefaultAsync();
    }

    public async Task SaveOrUpdateProfileAsync(UserProfile profile)
    {
        await InitAsync();
        if (_database == null) return;

        if (profile.Id == 0)
            await _database.InsertAsync(profile);
        else
            await _database.UpdateAsync(profile);
    }

    public async Task DeleteProfileAsync(UserProfile profile)
    {
        await InitAsync();
        if (_database != null)
        {
            await _database.DeleteAsync(profile);
        }
    }
}