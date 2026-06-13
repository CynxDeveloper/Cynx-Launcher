using System.Windows;
using System.Windows.Controls;
using SusanooLauncher.Services;
using SusanooLauncher.Theme;

namespace SusanooLauncher.Pages
{
    public partial class SettingsPage : Page
    {
        private bool _loadingSettings;

        public SettingsPage()
        {
            InitializeComponent();
            Loaded += (_, __) => LoadFromSettings();
        }

        private void LoadFromSettings()
        {
            _loadingSettings = true;
            SoundEffectsToggle.IsChecked = Settings.Default.soundEffects;
            BubbleBuildsToggle.IsChecked = Settings.Default.bubbleBuilds;
            LiveBackgroundToggle.IsChecked = Settings.Default.liveBackground;
            BackgroundShapeCombo.SelectedIndex = Settings.Default.liveBackgroundSquares ? 1 : 0;
            SacCodeBox.Text = LocalFeatureStore.Prefs.SupportACreator;
            StreamerModeToggle.IsChecked = LocalFeatureStore.Prefs.StreamerMode;
            HighContrastToggle.IsChecked = LocalFeatureStore.Prefs.HighContrast;
            _loadingSettings = false;
        }

        private void SacChanged(object sender, TextChangedEventArgs e)
        {
            if (_loadingSettings)
                return;
            LocalFeatureStore.Prefs.SupportACreator = SacCodeBox.Text.Trim();
            LocalFeatureStore.Save();
        }

        private void FeaturePrefChanged(object sender, RoutedEventArgs e)
        {
            if (_loadingSettings)
                return;
            LocalFeatureStore.Prefs.StreamerMode = StreamerModeToggle.IsChecked == true;
            LocalFeatureStore.Prefs.HighContrast = HighContrastToggle.IsChecked == true;
            LocalFeatureStore.Save();
        }

        private void SettingChanged(object sender, RoutedEventArgs e)
        {
            if (_loadingSettings)
                return;

            bool soundEnabled = SoundEffectsToggle.IsChecked == true;
            Settings.Default.soundEffects = soundEnabled;
            Settings.Default.bubbleBuilds = BubbleBuildsToggle.IsChecked == true;
            Settings.Default.liveBackground = LiveBackgroundToggle.IsChecked == true;
            Settings.Default.Save();
            ThemeManager.ApplyFromSettings();
        }

        private void BackgroundShapeChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!IsLoaded || _loadingSettings)
                return;

            Settings.Default.liveBackgroundSquares = BackgroundShapeCombo.SelectedIndex == 1;
            Settings.Default.Save();
            ThemeManager.ApplyFromSettings();
        }

        private void LogOutClicked(object sender, RoutedEventArgs e)
        {
            Settings.Default.username = "";
            Settings.Default.password = "";
            Settings.Default.accessToken = "";
            Settings.Default.accountId = "";
            Settings.Default.displayName = "";
            Settings.Default.Save();
            UserSession.Clear();

            if (Application.Current.MainWindow is MainWindow main)
                main.LogOut();
        }
    }
}
