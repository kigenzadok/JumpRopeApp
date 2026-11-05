using Microsoft.Maui.Controls;
using System.Threading.Tasks;

namespace JumpRopeApp;

public partial class AchievementsPage : ContentPage
{
    private readonly DatabaseService _dbService = new();

    public AchievementsPage()
    {
        InitializeComponent();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadGamificationDataAsync();
    }

    private async Task LoadGamificationDataAsync()
    {
        var (level, currentXp, nextLevelXp, progress) = await _dbService.GetUserLevelAsync();
        LevelNumberLabel.Text = $"Level {level}";
        XpRatioLabel.Text = $"{currentXp:N0} / {nextLevelXp:N0} XP";
        XpProgressBar.Progress = progress;

        var badges = await _dbService.GetAchievementsAsync();
        BadgesCollectionView.ItemsSource = badges;
    }
}