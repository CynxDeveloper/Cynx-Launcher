using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using SusanooLauncher.Services;
using SusanooLauncher.Theme;
using Wpf.Ui.Appearance;
using Wpf.Ui.Controls;

namespace SusanooLauncher
{
    public partial class MainWindow : FluentWindow
    {
        private enum NavTab
        {
            Home,
            Library,
            Shop,
            Leaderboard,
            Servers,
            Chat,
            News,
            Features,
            Profile,
            Settings,
        }

        private static readonly SolidColorBrush NavActiveBrush = new(Color.FromRgb(42, 49, 69));
        private static readonly SolidColorBrush NavIdleBrush = Brushes.Transparent;

        public MainWindow()
        {
            ApplicationThemeManager.ApplySystemTheme(true);
            InitializeComponent();

            ThemeManager.Background = LiveBg;
            ThemeManager.ApplyFromSettings();

            PreviewMouseLeftButtonDown += OnGlobalClick;
            KeyDown += OnKeyDown;
            FeatureNavigation.NavigateRequested += NavigateFeature;

            UserSession.LoadFromSettings();
            WireLoginPage(new Pages.LoginPage());
        }

        private void OnKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.F5 && MainFrame.Content is Pages.ShopPage)
            {
                MainFrame.Navigate(new Pages.ShopPage());
                e.Handled = true;
            }
        }

        private void NavigateFeature(string key)
        {
            Page? page = FeatureNavigation.CreatePage(key);
            if (page != null)
                MainFrame.Navigate(page);
        }

        private void WireLoginPage(Pages.LoginPage login)
        {
            login.LoggedIn += async (_, __) => await OnLoggedInAsync();
            LoginFrame.Navigate(login);
        }

        private async Task OnLoggedInAsync()
        {
            LoginRoot.Visibility = Visibility.Collapsed;
            ShellRoot.Visibility = Visibility.Collapsed;

            string displayName = Settings.Default.displayName;
            if (string.IsNullOrWhiteSpace(displayName))
                displayName = Settings.Default.username;

            await SyncOverlay.PlayAsync(
                displayName,
                UserSession.SkinIconUrl,
                UserSession.SkinName,
                UserSession.SkinTemplateId);

            _ = LauncherUpdateService.PromptAndApplyIfAvailableAsync(this);

            ShellRoot.Visibility = Visibility.Visible;
            ThemeManager.ApplyFromSettings();
            NavigateTo(NavTab.Home);
        }

        public void LogOut()
        {
            LocalFeatureStore.EndPlaySession();
            UserSession.Clear();
            ShellRoot.Visibility = Visibility.Collapsed;
            LoginRoot.Visibility = Visibility.Visible;
            WireLoginPage(new Pages.LoginPage());
        }

        private void HomeClicked(object sender, RoutedEventArgs e) => NavigateTo(NavTab.Home);
        private void LibraryClicked(object sender, RoutedEventArgs e) => NavigateTo(NavTab.Library);
        private void ShopClicked(object sender, RoutedEventArgs e) => NavigateTo(NavTab.Shop);
        private void LeaderboardClicked(object sender, RoutedEventArgs e) => NavigateTo(NavTab.Leaderboard);
        private void SettingsClicked(object sender, RoutedEventArgs e) => NavigateTo(NavTab.Settings);
        private void ServersClicked(object sender, RoutedEventArgs e) => NavigateTo(NavTab.Servers);
        private void ChatClicked(object sender, RoutedEventArgs e) => NavigateTo(NavTab.Chat);
        private void NewsClicked(object sender, RoutedEventArgs e) => NavigateTo(NavTab.News);
        private void FeaturesClicked(object sender, RoutedEventArgs e) => NavigateTo(NavTab.Features);
        private void ProfileClicked(object sender, RoutedEventArgs e) => NavigateTo(NavTab.Profile);

        private void OnGlobalClick(object sender, MouseButtonEventArgs e)
        {
            if (!Settings.Default.soundEffects)
                return;

            if (IsTextEntryClick(e.OriginalSource as DependencyObject))
                return;

            LauncherSoundService.PlayNavigation();
        }

        private static bool IsTextEntryClick(DependencyObject? source)
        {
            while (source != null)
            {
                if (source is TextBoxBase or System.Windows.Controls.PasswordBox)
                    return true;
                source = VisualTreeHelper.GetParent(source) ?? LogicalTreeHelper.GetParent(source);
            }

            return false;
        }

        private void NavigateTo(NavTab tab)
        {
            switch (tab)
            {
                case NavTab.Home:
                    MainFrame.Navigate(new Pages.HomePage());
                    break;
                case NavTab.Library:
                    MainFrame.Navigate(new Pages.LibraryPage());
                    break;
                case NavTab.Shop:
                    MainFrame.Navigate(new Pages.ShopPage());
                    break;
                case NavTab.Leaderboard:
                    MainFrame.Navigate(new Pages.LeaderboardPage());
                    break;
                case NavTab.Servers:
                    MainFrame.Navigate(new Pages.ServersPage());
                    break;
                case NavTab.Chat:
                    MainFrame.Navigate(new Pages.GlobalChatPage());
                    break;
                case NavTab.News:
                    MainFrame.Navigate(new Pages.NewsPage());
                    break;
                case NavTab.Features:
                    MainFrame.Navigate(new Pages.FeaturesHubPage());
                    break;
                case NavTab.Profile:
                    MainFrame.Navigate(new Pages.ProfilePage());
                    break;
                case NavTab.Settings:
                    MainFrame.Navigate(new Pages.SettingsPage());
                    break;
            }

            SetNavButton(HomeBtn, tab == NavTab.Home);
            SetNavButton(LibraryBtn, tab == NavTab.Library);
            SetNavButton(ShopBtn, tab == NavTab.Shop);
            SetNavButton(LeaderboardBtn, tab == NavTab.Leaderboard);
            SetNavButton(ServersBtn, tab == NavTab.Servers);
            SetNavButton(ChatBtn, tab == NavTab.Chat);
            SetNavButton(NewsBtn, tab == NavTab.News);
            SetNavButton(FeaturesBtn, tab == NavTab.Features);
            SetNavButton(ProfileBtn, tab == NavTab.Profile);
            SetNavButton(SettingsBtn, tab == NavTab.Settings);
        }

        private static void SetNavButton(Wpf.Ui.Controls.Button btn, bool active)
        {
            btn.Background = active ? NavActiveBrush : NavIdleBrush;
            btn.Appearance = active ? ControlAppearance.Primary : ControlAppearance.Secondary;
        }
    }
}
