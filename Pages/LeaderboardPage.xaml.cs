using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Threading;
using SusanooLauncher.Models;
using SusanooLauncher.Services;
using Image = System.Windows.Controls.Image;

namespace SusanooLauncher.Pages
{
    public partial class LeaderboardPage : Page
    {
        private static readonly Uri DefaultAvatarUri = new("pack://application:,,,/Assets/susanoo.png");

        private static readonly Brush RowGold = new SolidColorBrush(Color.FromArgb(38, 255, 213, 79));
        private static readonly Brush RowSilver = new SolidColorBrush(Color.FromArgb(32, 192, 192, 192));
        private static readonly Brush RowBronze = new SolidColorBrush(Color.FromArgb(32, 205, 127, 50));

        private readonly LeaderboardService _service = new();
        private readonly DispatcherTimer _refreshTimer = new();
        private readonly DispatcherTimer _countdownTimer = new();
        private List<LeaderboardEntry> _allEntries = [];
        private string _currentSort = "points";
        private int _secondsUntilRefresh = 3600;
        private int _refreshTotalSeconds = 3600;
        private CancellationTokenSource? _loadCts;

        public LeaderboardPage()
        {
            InitializeComponent();
            Loaded += async (_, __) =>
            {
                _refreshTotalSeconds = LauncherConfig.Load().LeaderboardRefreshSeconds;
                _secondsUntilRefresh = _refreshTotalSeconds;
                _refreshTimer.Interval = TimeSpan.FromSeconds(_refreshTotalSeconds);
                _refreshTimer.Tick += async (_, __) => await LoadAsync();
                _countdownTimer.Interval = TimeSpan.FromSeconds(1);
                _countdownTimer.Tick += (_, __) => TickCountdown();
                _countdownTimer.Start();
                UpdateProgressRing();
                await LoadAsync();
                _refreshTimer.Start();
            };
            Unloaded += (_, __) =>
            {
                _refreshTimer.Stop();
                _countdownTimer.Stop();
                _loadCts?.Cancel();
            };
        }

        private void TickCountdown()
        {
            if (_secondsUntilRefresh > 0)
                _secondsUntilRefresh--;

            int m = _secondsUntilRefresh / 60;
            int s = _secondsUntilRefresh % 60;
            UpdateTimerText.Text = $"Next update in {m}:{s:D2}";
            UpdateProgressRing();

            if (_secondsUntilRefresh <= 0)
                _ = LoadAsync();
        }

        private void UpdateProgressRing()
        {
            const double circumference = 2 * Math.PI * 10;
            double progress = _refreshTotalSeconds > 0
                ? (double)_secondsUntilRefresh / _refreshTotalSeconds
                : 0;
            RefreshProgressRing.StrokeDashArray = new DoubleCollection
            {
                circumference * progress,
                circumference,
            };
        }

        private async void TabPointsClicked(object sender, RoutedEventArgs e) => await SetSortAsync("points");
        private async void TabKillsClicked(object sender, RoutedEventArgs e) => await SetSortAsync("kills");
        private async void TabWinsClicked(object sender, RoutedEventArgs e) => await SetSortAsync("wins");

        private async Task SetSortAsync(string sort)
        {
            _currentSort = sort;
            TabPoints.IsChecked = sort == "points";
            TabKills.IsChecked = sort == "kills";
            TabWins.IsChecked = sort == "wins";
            await LoadAsync();
        }

        private void SearchClicked(object sender, RoutedEventArgs e) => ApplyFilter();
        private void SearchTextChanged(object sender, TextChangedEventArgs e) => ApplyFilter();

        private async Task LoadAsync()
        {
            _loadCts?.Cancel();
            _loadCts = new CancellationTokenSource();
            var token = _loadCts.Token;

            LoadingText.Visibility = Visibility.Visible;
            ErrorText.Visibility = Visibility.Collapsed;

            try
            {
                var (entries, nextSec) = await _service.FetchAsync(_currentSort, cancellationToken: token);
                if (token.IsCancellationRequested)
                    return;

                _allEntries = entries.ToList();
                _refreshTotalSeconds = nextSec > 0 ? nextSec : LauncherConfig.Load().LeaderboardRefreshSeconds;
                _secondsUntilRefresh = _refreshTotalSeconds;
                _refreshTimer.Interval = TimeSpan.FromSeconds(_refreshTotalSeconds);
                UpdateProgressRing();

                if (!string.IsNullOrWhiteSpace(_service.LastError) && _allEntries.Count == 0)
                {
                    ErrorText.Text = _service.LastError;
                    ErrorText.Visibility = Visibility.Visible;
                }

                ApplyFilter();
            }
            catch (OperationCanceledException)
            {
                // ignore
            }
            catch (Exception ex)
            {
                ErrorText.Text = ex.Message;
                ErrorText.Visibility = Visibility.Visible;
                LeaderboardList.Children.Clear();
            }
            finally
            {
                LoadingText.Visibility = Visibility.Collapsed;
            }
        }

        private void ApplyFilter()
        {
            string q = (SearchBox.Text ?? "").Trim();
            IEnumerable<LeaderboardEntry> filtered = _allEntries;
            if (!string.IsNullOrWhiteSpace(q))
                filtered = _allEntries.Where(e => e.DisplayName.Contains(q, StringComparison.OrdinalIgnoreCase));

            var list = filtered.ToList();
            for (int i = 0; i < list.Count; i++)
                list[i].Rank = i + 1;

            RenderRows(list);
            UpdatePodiumFromList(list);
        }

        private void UpdatePodiumFromList(IReadOnlyList<LeaderboardEntry> list)
        {
            void SetPodium(Image skinImage, TextBlock name, TextBlock score, LeaderboardEntry? e, double diameter)
            {
                if (e == null)
                {
                    name.Text = "—";
                    score.Text = "0";
                    SetSkinImage(skinImage, null, diameter);
                    return;
                }
                name.Text = e.DisplayName;
                score.Text = e.HypeArenaPoints.ToString("N0");
                SetSkinImage(skinImage, e, diameter);
            }

            SetPodium(Podium1Skin, Podium1Name, Podium1Score, list.Count > 0 ? list[0] : null, 96);
            SetPodium(Podium2Skin, Podium2Name, Podium2Score, list.Count > 1 ? list[1] : null, 76);
            SetPodium(Podium3Skin, Podium3Name, Podium3Score, list.Count > 2 ? list[2] : null, 76);
        }

        private void RenderRows(IReadOnlyList<LeaderboardEntry> entries)
        {
            LeaderboardList.Children.Clear();

            if (entries.Count == 0)
            {
                LeaderboardList.Children.Add(new TextBlock
                {
                    Text = "No players ranked yet.\nPlay Arena on Susanoo to earn Hype points.",
                    Foreground = new SolidColorBrush(Color.FromArgb(136, 255, 255, 255)),
                    Margin = new Thickness(20, 28, 20, 28),
                    TextAlignment = TextAlignment.Center,
                    TextWrapping = TextWrapping.Wrap,
                });
                return;
            }

            foreach (LeaderboardEntry entry in entries)
            {
                var row = new Border
                {
                    Padding = new Thickness(18, 11, 18, 11),
                    Background = GetRowBackground(entry.Rank),
                    Child = BuildRowGrid(entry),
                };
                LeaderboardList.Children.Add(row);
            }
        }

        private static Brush GetRowBackground(int rank) => rank switch
        {
            1 => RowGold,
            2 => RowSilver,
            3 => RowBronze,
            _ => Brushes.Transparent,
        };

        private Grid BuildRowGrid(LeaderboardEntry entry)
        {
            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(56) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(2.2, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            Brush rankColor = entry.Rank switch
            {
                1 => new SolidColorBrush(Color.FromRgb(255, 213, 79)),
                2 => new SolidColorBrush(Color.FromRgb(192, 192, 192)),
                3 => new SolidColorBrush(Color.FromRgb(205, 127, 50)),
                _ => new SolidColorBrush(Color.FromArgb(180, 255, 255, 255)),
            };

            var rank = new TextBlock
            {
                Text = $"#{entry.Rank}",
                Foreground = rankColor,
                FontWeight = FontWeights.SemiBold,
                FontSize = 13,
                VerticalAlignment = VerticalAlignment.Center,
            };
            Grid.SetColumn(rank, 0);
            grid.Children.Add(rank);

            var playerPanel = new Grid { VerticalAlignment = VerticalAlignment.Center };
            playerPanel.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            playerPanel.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            const double avatarSize = 44;
            var skinImage = CreateSkinImage(entry);
            skinImage.Width = avatarSize;
            skinImage.Height = avatarSize;
            SkinImageHelper.EnsureCircularClip(skinImage, avatarSize);

            var skinHost = new Grid
            {
                Width = avatarSize,
                Height = avatarSize,
                Margin = new Thickness(0, 0, 12, 0),
            };
            skinHost.Children.Add(new Ellipse { Fill = new SolidColorBrush(Color.FromRgb(20, 24, 36)) });
            skinHost.Children.Add(skinImage);
            skinHost.Children.Add(new Ellipse
            {
                Width = avatarSize,
                Height = avatarSize,
                Stroke = new SolidColorBrush(Color.FromRgb(45, 52, 70)),
                StrokeThickness = 1.5,
                Fill = Brushes.Transparent,
            });

            var skinBorder = skinHost;
            Grid.SetColumn(skinBorder, 0);
            playerPanel.Children.Add(skinBorder);

            var nameRow = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
            nameRow.Children.Add(new TextBlock
            {
                Text = entry.DisplayName,
                Foreground = Brushes.White,
                FontWeight = FontWeights.SemiBold,
                FontSize = 14,
                VerticalAlignment = VerticalAlignment.Center,
                MaxWidth = 220,
                TextTrimming = TextTrimming.CharacterEllipsis,
            });
            nameRow.Children.Add(new Border
            {
                Background = new SolidColorBrush(Color.FromRgb(35, 42, 58)),
                CornerRadius = new CornerRadius(6),
                Padding = new Thickness(8, 3, 8, 3),
                Margin = new Thickness(10, 0, 0, 0),
                VerticalAlignment = VerticalAlignment.Center,
                Child = new TextBlock
                {
                    Text = $"Lvl {entry.Level}",
                    Foreground = new SolidColorBrush(Color.FromArgb(200, 255, 255, 255)),
                    FontSize = 11,
                },
            });
            Grid.SetColumn(nameRow, 1);
            playerPanel.Children.Add(nameRow);

            Grid.SetColumn(playerPanel, 1);
            grid.Children.Add(playerPanel);

            AddStatCell(grid, 2, entry.Kills.ToString("N0"), _currentSort == "kills");
            AddStatCell(grid, 3, entry.Wins.ToString("N0"), _currentSort == "wins");
            AddStatCell(grid, 4, entry.HypeArenaPoints.ToString("N0"), _currentSort == "points");

            return grid;
        }

        private static void AddStatCell(Grid grid, int column, string value, bool highlight)
        {
            var tb = new TextBlock
            {
                Text = value,
                Foreground = highlight ? Brushes.White : new SolidColorBrush(Color.FromArgb(220, 255, 255, 255)),
                FontWeight = highlight ? FontWeights.SemiBold : FontWeights.Normal,
                FontSize = 13,
                VerticalAlignment = VerticalAlignment.Center,
            };
            Grid.SetColumn(tb, column);
            grid.Children.Add(tb);
        }

        private static Image CreateSkinImage(LeaderboardEntry entry)
        {
            const double avatarSize = 44;
            var image = new Image
            {
                Stretch = Stretch.UniformToFill,
                Width = avatarSize,
                Height = avatarSize,
            };
            SetSkinImage(image, entry, avatarSize);
            return image;
        }

        private static void SetSkinImage(Image image, LeaderboardEntry? entry, double? diameter = null)
        {
            if (entry != null && !string.IsNullOrWhiteSpace(entry.SkinIconUrl))
            {
                try
                {
                    var bitmap = new BitmapImage();
                    bitmap.BeginInit();
                    bitmap.UriSource = new Uri(entry.SkinIconUrl, UriKind.Absolute);
                    bitmap.CacheOption = BitmapCacheOption.OnLoad;
                    bitmap.EndInit();
                    image.Source = bitmap;
                    image.ImageFailed += OnSkinImageFailed;
                    SkinImageHelper.EnsureCircularClip(image, diameter);
                    return;
                }
                catch
                {
                    // fall through
                }
            }

            image.Source = new BitmapImage(DefaultAvatarUri);
            SkinImageHelper.EnsureCircularClip(image, diameter);
        }

        private static void OnSkinImageFailed(object? sender, ExceptionRoutedEventArgs e)
        {
            if (sender is Image img)
            {
                img.ImageFailed -= OnSkinImageFailed;
                img.Source = new BitmapImage(DefaultAvatarUri);
                SkinImageHelper.EnsureCircularClip(img);
            }
        }
    }
}
