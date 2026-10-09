using SQLite;
using System.Collections.Generic;

namespace JumpRopeApp.Models;

public class Routine
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}

public class RoutineStep
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }
    public int RoutineId { get; set; }
    public int StepOrder { get; set; }
    public int JumpDurationSecs { get; set; }
    public int RestDurationSecs { get; set; }
    public int TargetRpm { get; set; } = 120;
}