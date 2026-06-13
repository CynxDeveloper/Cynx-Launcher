using System.Windows;
using System.Windows.Controls;
using SusanooLauncher.Models;
using SusanooLauncher.Services;

namespace SusanooLauncher.Pages
{
    public partial class StatsPage : Page
    {
        private readonly LeaderboardService _leaderboard = new();

        public StatsPage()
        {
            InitializeComponent();
            Loaded += async (_, __) => await LoadAsync();
        }

        private async void RefreshClicked(object sender, RoutedEventArgs e) => await LoadAsync();

        private async Task LoadAsync()
        {
            long seconds = LocalFeatureStore.Prefs.TotalPlayTimeSeconds;
            if (LocalFeatureStore.Prefs.SessionStartedUtc != null)
                seconds += (long)(DateTime.UtcNow - LocalFeatureStore.Prefs.SessionStartedUtc.Value).TotalSeconds;

            TimeSpan t = TimeSpan.FromSeconds(seconds);
            PlayTimeText.Text = t.TotalHours >= 1 ? $"{(int)t.TotalHours}h {t.Minutes}m" : $"{t.Minutes}m";

            var unlocked = LocalFeatureStore.Prefs.Achievements.Where(kv => kv.Value).Select(kv => kv.Key).ToList();
            AchievementsText.Text = unlocked.Count > 0
                ? string.Join(", ", unlocked)
                : "Launch the game to unlock achievements.";

            PointsText.Text = KillsText.Text = WinsText.Text = "—";

            try
            {
                var (entries, _) = await _leaderboard.FetchAsync("points");
                string? me = UserSession.DisplayName ?? UserSession.Email;
                LeaderboardEntry? self = entries.FirstOrDefault(e =>
                    e.DisplayName.Equals(me, StringComparison.OrdinalIgnoreCase));

                if (self != null)
                {
                    PointsText.Text = self.HypeArenaPoints.ToString("N0");
                    KillsText.Text = self.Kills.ToString("N0");
                    WinsText.Text = self.Wins.ToString("N0");
                }
            }
            catch { }
        }
    }
}
