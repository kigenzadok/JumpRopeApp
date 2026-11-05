using SQLite;

namespace JumpRopeApp.Models;

public class RoutineRecord
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int TargetSteps { get; set; }
    public int TargetJumps { get; set; }
}