using SQLite;
using System;

namespace JumpRopeApp.Models;

public class WorkoutRecord
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }
    public DateTime Date { get; set; } = DateTime.Now;
    public string WorkoutType { get; set; } = "Jump Rope";
    public int SetsCompleted { get; set; }
    public int TotalJumps { get; set; }
    public int DurationSeconds { get; set; }
    public double CaloriesBurned { get; set; }

    public int JumpSecs { get; set; }
    public int RestSecs { get; set; }
}