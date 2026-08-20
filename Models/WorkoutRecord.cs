using SQLite;
using System;

namespace JumpRopeApp.Models;

[Table("Workouts")]
public class WorkoutRecord
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    public DateTime Date { get; set; }

    public int SetsCompleted { get; set; }

    public int TotalJumps { get; set; }

    public int JumpSecs { get; set; }

    public int RestSecs { get; set; }

    // New Fields for Diverse Fitness Sets
    public string WorkoutType { get; set; } = "Basic Jumps"; // e.g., "Speed Sets", "Tabata", "Mixed Circuit"
    public string TargetSkills { get; set; } = "Basic Bounce"; // e.g., "Double Unders, High Knees"

    [Ignore]
    public string DateFormatted => Date.ToString("MMM dd, yyyy");
    public string DateString { get; set; } = DateTime.Now.ToString("yyyy-MM-dd");

}