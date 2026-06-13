using System;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using SusanooLauncher.Services;
using Microsoft.Win32;
using TextBlock = System.Windows.Controls.TextBlock;

namespace SusanooLauncher.Pages
{
    public partial class HomePage : Page
    {
        private readonly DispatcherTimer _refreshTimer = new DispatcherTimer();
        private readonly FortniteLaunchService _svc = new FortniteLaunchService();
        private bool _launching;

        public HomePage()
        {
            InitializeComponent();
            UserSession.LoadFromSettings();
            UpdateWelcomeName();
            PathText.Text = string.IsNullOrWhiteSpace(Settings.Default.path) ? "(not set)" : Settings.Default.path;

            Loaded += OnLoaded;
            Unloaded += OnUnloaded;
            _refreshTimer.Interval = TimeSpan.FromSeconds(30);
            _refreshTimer.Tick += async (_, __) => await RefreshDashboardAsync();
        }

        private void UpdateWelcomeName()
        {
            string name = Settings.Default.displayName;
            if (string.IsNullOrWhiteSpace(name))
                name = Settings.Default.username;
            WelcomeNameText.Text = GetDisplayName(name);
        }

        private static string GetDisplayName(string? raw)
        {
            string value = (raw ?? "").Trim();
            if (string.IsNullOrWhiteSpace(value))
                return "Player";

            int at = value.IndexOf('@');
            if (at > 0)
                return value[..at];

            return value;
        }

        private async void OnLoaded(object sender, RoutedEventArgs e)
        {
            await ApplyPlayerSkinAsync();
            await RefreshDashboardAsync();
            _refreshTimer.Start();
        }

        private async Task ApplyPlayerSkinAsync()
        {
            if (string.IsNullOrWhiteSpace(UserSession.SkinIconUrl) && !string.IsNullOrWhiteSpace(UserSession.AccessToken))
            {
                PlayerSkinInfo skin = await PlayerSkinService.FetchAsync();
                UserSession.ApplySkin(skin.SkinName, skin.SkinIconUrl, skin.SkinTemplateId);
            }

            await HeroAvatar.ApplySkinAsync(UserSession.SkinIconUrl, UserSession.SkinTemplateId);

            if (!string.IsNullOrWhiteSpace(UserSession.SkinName))
                EquippedSkinText.Text = $"Equipped: {UserSession.SkinName}";
            else
                EquippedSkinText.Text = "Equipped: Default skin";
        }

        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
            _refreshTimer.Stop();
        }

        private async void RefreshOnlineClicked(object sender, RoutedEventArgs e) => await RefreshDashboardAsync();

        private async Task RefreshDashboardAsync()
        {
            OnlineCountText.Text = "Loading...";
            await Task.WhenAll(LoadNewsAsync(), LoadUpcomingAsync(), LoadServerStatusAsync());
        }

        private async Task LoadServerStatusAsync()
        {
            try
            {
                LauncherStatus status = await LauncherStatusService.FetchAsync();
                if (status.Maintenance)
                {
                    OnlineCountText.Text = "Maintenance";
                    return;
                }

                OnlineCountText.Text = status.PlayersOnline == 1
                    ? "1 player online"
                    : $"{status.PlayersOnline} players online";
            }
            catch
            {
                OnlineCountText.Text = "Server offline";
            }
        }

        private async Task LoadNewsAsync()
        {
            NewsPanel.Children.Clear();
            NewsPanel.Children.Add(MakeMutedText("Loading news..."));

            IReadOnlyList<NewsItem> news = await NewsService.FetchMotdAsync();
            NewsPanel.Children.Clear();

            if (news.Count == 0)
            {
                NewsPanel.Children.Add(MakeMutedText("No announcements from the backend."));
                return;
            }

            int shown = 0;
            foreach (NewsItem item in news)
            {
                if (shown >= 3)
                    break;

                var card = new Border
                {
                    CornerRadius = new CornerRadius(10),
                    Background = new SolidColorBrush(Color.FromArgb(40, 0, 0, 0)),
                    Padding = new Thickness(12),
                    Margin = new Thickness(0, 0, 0, 8),
                };
                var sp = new StackPanel();
                if (!string.IsNullOrWhiteSpace(item.TabTitle))
                {
                    sp.Children.Add(new TextBlock
                    {
                        Text = item.TabTitle.ToUpperInvariant(),
                        FontSize = 11,
                        Foreground = new SolidColorBrush(Color.FromArgb(160, 255, 255, 255)),
                    });
                }
                sp.Children.Add(new TextBlock
                {
                    Text = string.IsNullOrWhiteSpace(item.Title) ? "Announcement" : item.Title,
                    FontSize = 14,
                    FontWeight = FontWeights.SemiBold,
                    Foreground = Brushes.White,
                    TextWrapping = TextWrapping.Wrap,
                });
                if (!string.IsNullOrWhiteSpace(item.Body))
                {
                    string body = item.Body.Length > 140 ? item.Body[..140] + "…" : item.Body;
                    sp.Children.Add(new TextBlock
                    {
                        Text = body,
                        Margin = new Thickness(0, 4, 0, 0),
                        FontSize = 12,
                        Foreground = new SolidColorBrush(Color.FromArgb(200, 255, 255, 255)),
                        TextWrapping = TextWrapping.Wrap,
                    });
                }
                card.Child = sp;
                NewsPanel.Children.Add(card);
                shown++;
            }
        }

        private async Task LoadUpcomingAsync()
        {
            UpcomingPanel.Children.Clear();
            UpcomingPanel.Children.Add(MakeMutedText("Loading..."));

            LauncherStatus status = await LauncherStatusService.FetchAsync();
            UpcomingPanel.Children.Clear();

            var features = new List<UpcomingFeature>();
            features.AddRange(status.LiveFeatures);
            features.AddRange(status.UpcomingFeatures);

            if (features.Count == 0)
            {
                UpcomingPanel.Children.Add(MakeMutedText("No features listed from backend."));
                return;
            }

            int shown = 0;
            foreach (UpcomingFeature feat in features)
            {
                if (shown >= 6)
                    break;

                var row = new Grid { Margin = new Thickness(0, 0, 0, 10) };
                row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

                var sp = new StackPanel();
                sp.Children.Add(new TextBlock
                {
                    Text = feat.Name,
                    FontSize = 14,
                    FontWeight = FontWeights.SemiBold,
                    Foreground = Brushes.White,
                });
                if (!string.IsNullOrWhiteSpace(feat.Description))
                {
                    sp.Children.Add(new TextBlock
                    {
                        Text = feat.Description,
                        Margin = new Thickness(0, 2, 0, 0),
                        FontSize = 12,
                        Foreground = new SolidColorBrush(Color.FromArgb(180, 255, 255, 255)),
                        TextWrapping = TextWrapping.Wrap,
                    });
                }
                Grid.SetColumn(sp, 0);
                row.Children.Add(sp);

                bool isLive = feat.Status.Equals("Live", StringComparison.OrdinalIgnoreCase);
                var badge = new Border
                {
                    CornerRadius = new CornerRadius(8),
                    Background = new SolidColorBrush(isLive ? Color.FromRgb(34, 80, 55) : Color.FromRgb(42, 49, 69)),
                    Padding = new Thickness(8, 4, 8, 4),
                    VerticalAlignment = VerticalAlignment.Top,
                    Child = new TextBlock
                    {
                        Text = feat.Status,
                        FontSize = 11,
                        Foreground = new SolidColorBrush(isLive ? Color.FromRgb(120, 220, 160) : Color.FromRgb(200, 210, 230)),
                    },
                };
                Grid.SetColumn(badge, 1);
                row.Children.Add(badge);
                UpcomingPanel.Children.Add(row);
                shown++;
            }
        }

        private static TextBlock MakeMutedText(string text) =>
            new()
            {
                Text = text,
                FontSize = 12,
                Foreground = new SolidColorBrush(Color.FromArgb(140, 255, 255, 255)),
                TextWrapping = TextWrapping.Wrap,
            };

        private void OpenDiscordClicked(object sender, RoutedEventArgs e)
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = "https://discord.gg/qNXVhkDwfH",
                UseShellExecute = true
            });
        }

        private void SelectFolderClicked(object sender, RoutedEventArgs e)
        {
            OpenFolderDialog dialog = new OpenFolderDialog
            {
                Title = "Select Fortnite Folder",
                InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86)
            };
            if (dialog.ShowDialog() == true)
            {
                Settings.Default.path = dialog.FolderName;
                Settings.Default.Save();
                PathText.Text = Settings.Default.path;
            }
        }

        private void LaunchBarClicked(object sender, MouseButtonEventArgs e) => _ = LaunchAsync();

        private void SetStatus(string text)
        {
            try
            {
                if (!IsLoaded || Dispatcher.HasShutdownStarted)
                    return;

                if (!Dispatcher.CheckAccess())
                {
                    Dispatcher.BeginInvoke(() => SetStatus(text));
                    return;
                }

                StatusText.Text = text;
            }
            catch { }
        }

        private async Task LaunchAsync()
        {
            if (_launching)
                return;

            if (string.IsNullOrWhiteSpace(Settings.Default.path))
            {
                MessageBox.Show("Select your Fortnite build folder first (Change button above Launch).", "Susanoo Launcher");
                return;
            }

            _launching = true;
            SetStatus("Launching...");

            try
            {
                bool ok = await _svc.LaunchAsync(
                    basePath: Settings.Default.path,
                    authLogin: Settings.Default.username,
                    authPassword: Settings.Default.password,
                    authType: "epic",
                    backendUrl: UserSession.BackendUrl,
                    status: SetStatus);

                if (!ok && !string.IsNullOrWhiteSpace(_svc.LastError))
                    MessageBox.Show(_svc.LastError, "Susanoo Launcher");

                SetStatus(ok ? "Ready" : "Launch failed");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Launch failed unexpectedly:\n\n{ex.Message}", "Susanoo Launcher");
                SetStatus("Ready");
            }
            finally
            {
                _launching = false;
            }
        }
    }
}
