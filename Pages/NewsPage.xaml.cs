using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using SusanooLauncher.Services;

namespace SusanooLauncher.Pages
{
    public partial class NewsPage : Page
    {
        public NewsPage()
        {
            InitializeComponent();
            Loaded += async (_, __) => await LoadAsync();
        }

        private async void RefreshClicked(object sender, RoutedEventArgs e) => await LoadAsync();

        private async Task LoadAsync()
        {
            LoadingText.Visibility = Visibility.Visible;
            NewsPanel.Children.Clear();

            bool online = await NewsService.IsBackendOnlineAsync();
            var news = await NewsService.FetchMotdAsync();

            LoadingText.Visibility = Visibility.Collapsed;

            NewsPanel.Children.Add(new TextBlock
            {
                Text = online ? "● Backend online" : "● Backend offline — showing cached message",
                Foreground = online
                    ? new SolidColorBrush(Color.FromRgb(34, 197, 94))
                    : new SolidColorBrush(Color.FromRgb(255, 138, 138)),
                Margin = new Thickness(0, 0, 0, 12),
            });

            foreach (NewsItem item in news)
            {
                var card = new Border
                {
                    CornerRadius = new CornerRadius(14),
                    Background = new SolidColorBrush(Color.FromArgb(34, 0, 0, 0)),
                    Padding = new Thickness(18),
                    Margin = new Thickness(0, 0, 0, 12),
                    Child = new StackPanel(),
                };
                var sp = (StackPanel)card.Child;
                sp.Children.Add(new TextBlock
                {
                    Text = item.Title,
                    FontSize = 18,
                    FontWeight = FontWeights.SemiBold,
                    Foreground = Brushes.White,
                    TextWrapping = TextWrapping.Wrap,
                });
                sp.Children.Add(new TextBlock
                {
                    Text = item.Body,
                    Margin = new Thickness(0, 8, 0, 0),
                    Foreground = new SolidColorBrush(Color.FromArgb(220, 255, 255, 255)),
                    TextWrapping = TextWrapping.Wrap,
                });
                NewsPanel.Children.Add(card);
            }
        }
    }
}
