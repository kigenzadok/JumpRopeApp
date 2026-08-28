using SQLite;

namespace JumpRopeApp.Models;

public class UserProfile
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public double WeightKg { get; set; }
    public double HeightCm { get; set; }

    // New Settings & Target Goal Fields
    public int DailyJumpGoal { get; set; } = 1000;
    public bool EnableHaptics { get; set; } = true;
    public bool EnableAudio { get; set; } = true;
}