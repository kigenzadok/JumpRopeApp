using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using JumpRopeApp.Models;

namespace JumpRopeApp.Services;

public class CloudService
{
    private readonly HttpClient _httpClient;

    // Your live Railway backend URL
    private const string BaseUrl = "https://your-railway-app-url.up.railway.app/api/";

    public CloudService()
    {
        _httpClient = new HttpClient
        {
            BaseAddress = new Uri(BaseUrl),
            Timeout = TimeSpan.FromSeconds(15)
        };
    }

    public async Task<bool> SyncWorkoutAsync(WorkoutRecord record)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("workouts", record);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Cloud sync failed: {ex.Message}");
            return false;
        }
    }

    public async Task<List<WorkoutRecord>> GetCloudWorkoutsAsync()
    {
        try
        {
            return await _httpClient.GetFromJsonAsync<List<WorkoutRecord>>("workouts") ?? new();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to fetch cloud workouts: {ex.Message}");
            return new();
        }
    }
}