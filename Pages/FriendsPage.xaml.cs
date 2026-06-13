using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using SusanooLauncher.Services;

namespace SusanooLauncher.Pages
{
    public partial class FriendsPage : Page
    {
        public FriendsPage()
        {
            InitializeComponent();
            Loaded += async (_, __) => await LoadAsync();
        }

        private async void RefreshClicked(object sender, RoutedEventArgs e) => await LoadAsync();

        private async Task LoadAsync()
        {
            FriendsList.Children.Clear();
            var friends = await FriendsService.FetchFriendsAsync();

            if (friends.Count == 0)
            {
                EmptyText.Visibility = Visibility.Visible;
                return;
            }

            EmptyText.Visibility = Visibility.Collapsed;
            foreach (FriendEntry f in friends)
            {
                var row = new Border
                {
                    Padding = new Thickness(14, 10, 14, 10),
                    Margin = new Thickness(0, 0, 0, 4),
                    CornerRadius = new CornerRadius(10),
                    Background = new SolidColorBrush(Color.FromRgb(26, 31, 46)),
                    Child = new Grid(),
                };
                var g = (Grid)row.Child;
                g.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(10) });
                g.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

                var dot = new Border
                {
                    Width = 8,
                    Height = 8,
                    CornerRadius = new CornerRadius(4),
                    Background = new SolidColorBrush(Color.FromRgb(34, 197, 94)),
                    VerticalAlignment = VerticalAlignment.Center,
                };
                g.Children.Add(dot);

                var name = new TextBlock
                {
                    Text = f.DisplayName,
                    Foreground = Brushes.White,
                    FontWeight = FontWeights.SemiBold,
                    Margin = new Thickness(12, 0, 0, 0),
                    VerticalAlignment = VerticalAlignment.Center,
                };
                Grid.SetColumn(name, 1);
                g.Children.Add(name);

                FriendsList.Children.Add(row);
            }
        }
    }
}
