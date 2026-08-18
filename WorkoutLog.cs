using SQLite;
using System;

namespace JumpRopeApp;

[Table("WorkoutLogs")]
public class WorkoutLog
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    [Indexed]
    public DateTime Date { get; set; }

    public int SetsCompleted { get; set; }
    public int TotalJumps { get; set; }
    public int JumpDurationSecs { get; set; }
    public int RestDurationSecs { get; set; }
}