using System.Windows.Controls;
using SusanooLauncher.Pages;

namespace SusanooLauncher.Services
{
    internal static class FeatureNavigation
    {
        public static event Action<string>? NavigateRequested;

        public static void Go(string navigateKey) => NavigateRequested?.Invoke(navigateKey);

        public static Page? CreatePage(string navigateKey) => navigateKey switch
        {
            "home" => new HomePage(),
            "library" => new LibraryPage(),
            "shop" => new ShopPage(),
            "news" => new NewsPage(),
            "friends" => new FriendsPage(),
            "profile" => new ProfilePage(),
            "stats" => new StatsPage(),
            "battlepass" => new BattlePassPage(),
            "quests" => new QuestsPage(),
            "leaderboard" => new LeaderboardPage(),
            "servers" => new ServersPage(),
            "chat" => new GlobalChatPage(),
            "downloads" => new DownloadsPage(),
            "settings" => new SettingsPage(),
            "features" => new FeaturesHubPage(),
            _ => null,
        };
    }
}
