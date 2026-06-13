using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using SusanooLauncher.Services;
using TextBlock = System.Windows.Controls.TextBlock;

namespace SusanooLauncher.Pages
{
    public partial class GlobalChatPage : Page
    {
        private readonly DispatcherTimer _poll = new() { Interval = TimeSpan.FromSeconds(3) };
        private long _lastSeenMs;
        private readonly HashSet<string> _seenIds = new(StringComparer.Ordinal);

        public GlobalChatPage()
        {
            InitializeComponent();
            Loaded += async (_, __) =>
            {
                await PollAsync();
                _poll.Tick += async (_, __) => await PollAsync();
                _poll.Start();
            };
            Unloaded += (_, __) => _poll.Stop();
        }

        private async void SendClicked(object sender, RoutedEventArgs e) => await SendAsync();

        private async void MessageKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                await SendAsync();
                e.Handled = true;
            }
        }

        private async Task SendAsync()
        {
            string text = (ChatInputBox.Text ?? "").Trim();
            if (string.IsNullOrWhiteSpace(text))
                return;

            var (ok, msg) = await GlobalChatService.SendAsync(text);
            if (!ok)
            {
                StatusText.Text = msg;
                return;
            }

            ChatInputBox.Text = "";
            await PollAsync();
        }

        private async Task PollAsync()
        {
            var (enabled, messages) = await GlobalChatService.FetchMessagesAsync(_lastSeenMs);
            if (!enabled)
            {
                StatusText.Text = "Global chat is disabled on the backend.";
                return;
            }

            StatusText.Text = "Connected to global chat";
            bool added = false;

            foreach (ChatMessage msg in messages.OrderBy(m => m.SentAt))
            {
                if (!_seenIds.Add(msg.Id))
                    continue;

                _lastSeenMs = Math.Max(_lastSeenMs, msg.SentAt);
                MessagesPanel.Children.Add(BuildBubble(msg));
                added = true;
            }

            if (added)
                ChatScroll.ScrollToEnd();
        }

        private static Border BuildBubble(ChatMessage msg)
        {
            var sp = new StackPanel();
            sp.Children.Add(new TextBlock
            {
                Text = msg.DisplayName,
                FontWeight = FontWeights.SemiBold,
                Foreground = new SolidColorBrush(Color.FromRgb(140, 198, 255)),
                FontSize = 12,
            });
            sp.Children.Add(new TextBlock
            {
                Text = msg.Text,
                Foreground = Brushes.White,
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 4, 0, 0),
            });

            return new Border
            {
                Child = sp,
                Margin = new Thickness(0, 0, 0, 10),
                Padding = new Thickness(12, 8, 12, 8),
                CornerRadius = new CornerRadius(10),
                Background = new SolidColorBrush(Color.FromRgb(26, 31, 46)),
                HorizontalAlignment = HorizontalAlignment.Left,
                MaxWidth = 520,
            };
        }
    }
}
