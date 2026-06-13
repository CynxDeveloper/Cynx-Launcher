using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using SusanooLauncher.Services;

namespace SusanooLauncher.Pages
{
    public partial class ServersPage : Page
    {
        public ServersPage()
        {
            InitializeComponent();
            _ = RefreshAsync();
        }

        private async void RefreshClicked(object sender, RoutedEventArgs e)
        {
            await RefreshAsync();
        }

        private async Task RefreshAsync()
        {
            try
            {
                SummaryText.Text = "Loading...";
                SessionsList.Items.Clear();

                GameServersSnapshot snapshot = await GameServersService.FetchAsync();

                if (snapshot.ServersOnline <= 0)
                {
                    SummaryText.Text = "No GameServers Online";
                    return;
                }

                SummaryText.Text =
                    $"{snapshot.ServersOnline} GameServer(s) Online · {snapshot.TotalPlayers} player(s)";

                foreach (GameServerSession session in snapshot.Sessions)
                    SessionsList.Items.Add(BuildSessionRow(session));
            }
            catch
            {
                SummaryText.Text = "No GameServers Online";
                SessionsList.Items.Clear();
            }
        }

        private static UIElement BuildSessionRow(GameServerSession session)
        {
            string title = !string.IsNullOrWhiteSpace(session.Title)
                ? session.Title
                : $"{session.PlaylistLabel} • {session.Status}";

            string meta = !string.IsNullOrWhiteSpace(session.Meta)
                ? session.Meta
                : $"{session.Region} • {session.SessionId}";

            string playersLeft = !string.IsNullOrWhiteSpace(session.PlayersLeftText)
                ? session.PlayersLeftText
                : $"{session.PlayersLeft} players left";

            string countPill = !string.IsNullOrWhiteSpace(session.PlayerCountText)
                ? session.PlayerCountText
                : $"{session.PlayerCount} / {session.MaxPlayers}";

            var root = new Border
            {
                Padding = new Thickness(0, 14, 0, 14),
                BorderBrush = new SolidColorBrush(Color.FromArgb(0x33, 0xFF, 0xFF, 0xFF)),
                BorderThickness = new Thickness(0, 0, 0, 1),
            };

            var stack = new StackPanel();

            var top = new Grid();
            top.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            top.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            top.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var titleBlock = new TextBlock
            {
                Text = title,
                Foreground = Brushes.White,
                FontSize = 15,
                FontWeight = FontWeights.SemiBold,
                TextTrimming = TextTrimming.CharacterEllipsis,
            };
            Grid.SetColumn(titleBlock, 0);
            top.Children.Add(titleBlock);

            var leftBlock = new TextBlock
            {
                Text = playersLeft,
                Foreground = new SolidColorBrush(Color.FromArgb(0x99, 0xFF, 0xFF, 0xFF)),
                FontSize = 13,
                Margin = new Thickness(12, 0, 10, 0),
                VerticalAlignment = VerticalAlignment.Center,
            };
            Grid.SetColumn(leftBlock, 1);
            top.Children.Add(leftBlock);

            var pillBorder = new Border
            {
                Background = new SolidColorBrush(Color.FromArgb(0x55, 0x20, 0x25, 0x35)),
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(10, 4, 10, 4),
                VerticalAlignment = VerticalAlignment.Center,
                Child = new TextBlock
                {
                    Text = countPill,
                    Foreground = Brushes.White,
                    FontSize = 13,
                    FontWeight = FontWeights.SemiBold,
                },
            };
            Grid.SetColumn(pillBorder, 2);
            top.Children.Add(pillBorder);

            stack.Children.Add(top);

            stack.Children.Add(new TextBlock
            {
                Text = meta,
                Margin = new Thickness(0, 6, 0, 0),
                Foreground = new SolidColorBrush(Color.FromArgb(0x88, 0xFF, 0xFF, 0xFF)),
                FontSize = 12,
                TextTrimming = TextTrimming.CharacterEllipsis,
            });

            root.Child = stack;
            return root;
        }
    }
}
