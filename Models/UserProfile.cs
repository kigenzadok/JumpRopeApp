using SQLite;
using System;

namespace JumpRopeApp.Models;

public class UserProfile
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public double WeightKg { get; set; }
    public double HeightCm { get; set; }
    public string FitnessGoal { get; set; } = "General Fitness";
    public DateTime CreatedAt { get; set; } = DateTime.Now;
}