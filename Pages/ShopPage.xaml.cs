using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using SusanooLauncher.Models;
using SusanooLauncher.Services;
using Image = System.Windows.Controls.Image;

namespace SusanooLauncher.Pages
{
    public partial class ShopPage : Page
    {
        private static readonly Uri PlaceholderUri = new("pack://application:,,,/Assets/susanoo.png");

        private readonly ItemShopService _service = new();
        private readonly DispatcherTimer _timer = new() { Interval = TimeSpan.FromSeconds(1) };
        private List<ShopItem> _allItems = [];
        private string _section = "Daily";
        private CancellationTokenSource? _loadCts;
        private bool _isReady;
        private int _vbucksBalance = -1;

        public ShopPage()
        {
            InitializeComponent();
            _timer.Tick += (_, __) => UpdateResetTimerLocal();
            Loaded += async (_, __) =>
            {
                _isReady = true;
                UpdateResetTimerLocal();
                _timer.Start();
                await LoadAsync();
            };
            Unloaded += (_, __) =>
            {
                _loadCts?.Cancel();
                _timer.Stop();
            };
        }

        private void UpdateResetTimerLocal()
        {
            if (!_isReady || ResetTimerText == null)
                return;

            DateTime now = DateTime.UtcNow;
            DateTime next = now.Date.AddDays(1);
            TimeSpan left = next - now;
            ResetTimerText.Text = $"Shop resets in {left.Hours:D2}:{left.Minutes:D2}:{left.Seconds:D2} (UTC)";
        }

        private void TabDailyClicked(object sender, RoutedEventArgs e)
        {
            _section = "Daily";
            TabDaily.IsChecked = true;
            TabFeatured.IsChecked = false;
            RenderItems();
        }

        private void TabFeaturedClicked(object sender, RoutedEventArgs e)
        {
            _section = "Featured";
            TabFeatured.IsChecked = true;
            TabDaily.IsChecked = false;
            RenderItems();
        }

        private void SearchChanged(object sender, TextChangedEventArgs e)
        {
            if (_isReady)
                RenderItems();
        }

        private void SortChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_isReady)
                RenderItems();
        }

        private async void RefreshClicked(object sender, RoutedEventArgs e) => await LoadAsync();

        private async Task LoadAsync()
        {
            try
            {
                _loadCts?.Cancel();
                _loadCts = new CancellationTokenSource();
                var token = _loadCts.Token;

                _vbucksBalance = await ShopPurchaseService.GetBalanceAsync();
                BalanceText.Text = _vbucksBalance >= 0
                    ? $"V-Bucks: {_vbucksBalance:N0}"
                    : "V-Bucks: —";

                LoadingText.Visibility = Visibility.Visible;
                ErrorText.Visibility = Visibility.Collapsed;
                ShopItemsPanel.Children.Clear();

                IReadOnlyList<ShopItem> items = await _service.FetchAsync(cancellationToken: token);
                if (token.IsCancellationRequested)
                    return;

                LoadingText.Visibility = Visibility.Collapsed;

                if (items.Count == 0)
                {
                    ErrorText.Text = _service.LastError ?? "No items in the shop.";
                    ErrorText.Visibility = Visibility.Visible;
                    _allItems = [];
                    return;
                }

                _allItems = items.ToList();
                RenderItems();
            }
            catch (Exception ex)
            {
                LoadingText.Visibility = Visibility.Collapsed;
                ErrorText.Text = ex.Message;
                ErrorText.Visibility = Visibility.Visible;
                _allItems = [];
            }
        }

        private IEnumerable<ShopItem> GetFilteredSorted()
        {
            IEnumerable<ShopItem> q = _allItems.Where(i =>
                string.Equals(i.Section, _section, StringComparison.OrdinalIgnoreCase));

            string search = (SearchBox?.Text ?? "").Trim();
            if (!string.IsNullOrWhiteSpace(search))
                q = q.Where(i => (i.Name ?? "").Contains(search, StringComparison.OrdinalIgnoreCase));

            int sort = SortCombo?.SelectedIndex ?? 0;
            return sort switch
            {
                1 => q.OrderBy(i => i.Price),
                2 => q.OrderByDescending(i => i.Price),
                3 => q.OrderBy(i => i.Name),
                _ => q,
            };
        }

        private void RenderItems()
        {
            if (!_isReady || ShopItemsPanel == null)
                return;

            UpdateResetTimerLocal();
            ShopItemsPanel.Children.Clear();

            foreach (ShopItem item in GetFilteredSorted())
            {
                try
                {
                    ShopItemsPanel.Children.Add(CreateItemCard(item));
                }
                catch
                {
                    // Skip a single bad card instead of crashing the page.
                }
            }

            if (ShopItemsPanel.Children.Count > 0)
                return;

            ShopItemsPanel.Children.Add(new TextBlock
            {
                Text = $"No {_section.ToLowerInvariant()} items match.",
                Foreground = new SolidColorBrush(Color.FromArgb(160, 255, 255, 255)),
                FontSize = 14,
                Margin = new Thickness(8),
            });
        }

        private Border CreateItemCard(ShopItem item)
        {
            var wishlist = LocalFeatureStore.Prefs.WishlistOfferIds ?? [];
            bool wished = !string.IsNullOrWhiteSpace(item.OfferId) && wishlist.Contains(item.OfferId);

            var card = new Border
            {
                Width = 168,
                Height = 240,
                Margin = new Thickness(0, 0, 14, 14),
                CornerRadius = new CornerRadius(14),
                Background = new SolidColorBrush(Color.FromRgb(26, 31, 46)),
                BorderBrush = RarityBrush(item.Rarity),
                BorderThickness = new Thickness(2),
                Child = new Grid(),
            };

            var grid = (Grid)card.Child!;
            var panel = new StackPanel();
            grid.Children.Add(panel);

            var wishBtn = new Button
            {
                Content = wished ? "★" : "☆",
                Width = 28,
                Height = 28,
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Top,
                Margin = new Thickness(0, 8, 8, 0),
                Background = Brushes.Transparent,
                BorderThickness = new Thickness(0),
                Foreground = wished ? Brushes.Gold : Brushes.White,
                FontSize = 16,
            };
            wishBtn.Click += (_, __) =>
            {
                if (string.IsNullOrWhiteSpace(item.OfferId))
                    return;
                var list = LocalFeatureStore.Prefs.WishlistOfferIds ??= [];
                if (list.Contains(item.OfferId))
                    list.Remove(item.OfferId);
                else
                    list.Add(item.OfferId);
                LocalFeatureStore.Save();
                RenderItems();
            };
            grid.Children.Add(wishBtn);

            panel.Children.Add(new Border
            {
                Height = 130,
                Margin = new Thickness(12, 12, 12, 0),
                CornerRadius = new CornerRadius(10),
                Background = new SolidColorBrush(Color.FromRgb(15, 18, 28)),
                ClipToBounds = true,
                Child = CreateItemImage(item),
            });

            panel.Children.Add(new TextBlock
            {
                Text = item.Name,
                Margin = new Thickness(12, 10, 12, 0),
                Foreground = Brushes.White,
                FontWeight = FontWeights.SemiBold,
                FontSize = 13,
                TextWrapping = TextWrapping.Wrap,
                MaxHeight = 36,
            });

            var priceRow = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(12, 8, 12, 4) };
            priceRow.Children.Add(new TextBlock
            {
                Text = item.Price.ToString("N0"),
                Foreground = new SolidColorBrush(Color.FromRgb(140, 198, 255)),
                FontWeight = FontWeights.Bold,
                FontSize = 15,
            });
            priceRow.Children.Add(new TextBlock
            {
                Text = " V-Bucks",
                Foreground = new SolidColorBrush(Color.FromArgb(200, 255, 255, 255)),
                FontSize = 12,
                VerticalAlignment = VerticalAlignment.Center,
            });
            panel.Children.Add(priceRow);

            bool canAfford = _vbucksBalance < 0 || _vbucksBalance >= item.Price;
            var buy = new Button
            {
                Content = canAfford ? "Buy" : "Not enough",
                Margin = new Thickness(12, 0, 12, 10),
                Height = 28,
                Background = new SolidColorBrush(canAfford ? Color.FromRgb(59, 130, 246) : Color.FromRgb(80, 80, 90)),
                Foreground = Brushes.White,
                BorderThickness = new Thickness(0),
                IsEnabled = canAfford && !string.IsNullOrWhiteSpace(item.OfferId),
            };
            buy.Click += async (_, __) => await PurchaseItemAsync(item);
            panel.Children.Add(buy);

            return card;
        }

        private async Task PurchaseItemAsync(ShopItem item)
        {
            if (string.IsNullOrWhiteSpace(item.OfferId))
                return;

            var confirm = System.Windows.MessageBox.Show(
                $"Buy \"{item.Name}\" for {item.Price:N0} V-Bucks?",
                "Susanoo Shop",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (confirm != MessageBoxResult.Yes)
                return;

            PurchaseResult result = await ShopPurchaseService.PurchaseAsync(item.OfferId);
            System.Windows.MessageBox.Show(
                result.Message,
                result.Success ? "Susanoo Shop" : "Purchase failed",
                MessageBoxButton.OK,
                result.Success ? MessageBoxImage.Information : MessageBoxImage.Warning);

            if (result.Success)
            {
                if (result.Balance >= 0)
                {
                    _vbucksBalance = result.Balance;
                    BalanceText.Text = $"V-Bucks: {_vbucksBalance:N0}";
                }
                await LoadAsync();
            }
        }

        private static Brush RarityBrush(string rarity) => (rarity ?? "").ToLowerInvariant() switch
        {
            "legendary" or "superrare" => new SolidColorBrush(Color.FromRgb(255, 183, 77)),
            "epic" => new SolidColorBrush(Color.FromRgb(180, 120, 255)),
            "rare" => new SolidColorBrush(Color.FromRgb(100, 180, 255)),
            "uncommon" => new SolidColorBrush(Color.FromRgb(120, 200, 120)),
            _ => new SolidColorBrush(Color.FromRgb(42, 49, 69)),
        };

        private static Image CreateItemImage(ShopItem item)
        {
            var image = new Image { Stretch = Stretch.Uniform, Margin = new Thickness(8) };
            if (!string.IsNullOrWhiteSpace(item.ImageUrl))
            {
                try
                {
                    var bitmap = new BitmapImage();
                    bitmap.BeginInit();
                    bitmap.UriSource = new Uri(item.ImageUrl, UriKind.Absolute);
                    bitmap.CacheOption = BitmapCacheOption.OnLoad;
                    bitmap.EndInit();
                    image.Source = bitmap;
                    image.ImageFailed += OnImageFailed;
                    return image;
                }
                catch { }
            }
            image.Source = new BitmapImage(PlaceholderUri);
            return image;
        }

        private static void OnImageFailed(object? sender, ExceptionRoutedEventArgs e)
        {
            if (sender is Image img)
            {
                img.ImageFailed -= OnImageFailed;
                img.Source = new BitmapImage(PlaceholderUri);
            }
        }
    }
}
