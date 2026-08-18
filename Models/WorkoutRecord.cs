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

    [Ignore]
    public string DateFormatted => Date.ToString("MMM dd, yyyy");
    public string DateString { get; set; } = DateTime.Now.ToString("yyyy-MM-dd");

}