using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using SusanooLauncher.Services;

namespace SusanooLauncher.Controls
{
    public partial class SyncOverlay : UserControl
    {
        private readonly DispatcherTimer _spinnerTimer = new();
        private Storyboard? _welcomeFadeIn;
        private Storyboard? _welcomeFadeOut;

        public SyncOverlay()
        {
            InitializeComponent();

            _spinnerTimer.Interval = TimeSpan.FromMilliseconds(16);
            _spinnerTimer.Tick += (_, __) =>
            {
                SpinnerRotate.Angle = (SpinnerRotate.Angle + 4) % 360;
            };
        }

        public async Task PlayAsync(
            string displayName,
            string? skinIconUrl = null,
            string? skinName = null,
            string? skinTemplateId = null)
        {
            Visibility = Visibility.Visible;
            SyncPanel.Visibility = Visibility.Visible;
            SyncPanel.Opacity = 1;
            WelcomePanel.Visibility = Visibility.Visible;
            WelcomePanel.Opacity = 0;
            WelcomeNameText.Text = string.IsNullOrWhiteSpace(displayName) ? "Player" : displayName.Trim();

            await Task.WhenAll(
                SyncAvatar.ApplySkinAsync(skinIconUrl, skinTemplateId),
                WelcomeAvatar.ApplySkinAsync(skinIconUrl, skinTemplateId));

            if (!string.IsNullOrWhiteSpace(skinName))
            {
                WelcomeSkinText.Text = $"Wearing: {skinName}";
                WelcomeSkinText.Visibility = Visibility.Visible;
            }
            else
            {
                WelcomeSkinText.Text = "";
                WelcomeSkinText.Visibility = Visibility.Collapsed;
            }

            _spinnerTimer.Start();

            await Task.Delay(2400);

            _spinnerTimer.Stop();

            var syncFade = new DoubleAnimation(1, 0, TimeSpan.FromMilliseconds(350));
            syncFade.Completed += (_, __) => SyncPanel.Visibility = Visibility.Collapsed;
            SyncPanel.BeginAnimation(OpacityProperty, syncFade);

            WelcomePanel.Opacity = 0;
            _welcomeFadeIn = new Storyboard();
            var fadeIn = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(450));
            Storyboard.SetTarget(fadeIn, WelcomePanel);
            Storyboard.SetTargetProperty(fadeIn, new PropertyPath(OpacityProperty));
            _welcomeFadeIn.Children.Add(fadeIn);
            _welcomeFadeIn.Begin();

            await Task.Delay(2000);

            var tcs = new TaskCompletionSource();
            _welcomeFadeOut = new Storyboard();
            var fadeOut = new DoubleAnimation(1, 0, TimeSpan.FromMilliseconds(400));
            Storyboard.SetTarget(fadeOut, WelcomePanel);
            Storyboard.SetTargetProperty(fadeOut, new PropertyPath(OpacityProperty));
            fadeOut.Completed += (_, __) => tcs.TrySetResult();
            _welcomeFadeOut.Children.Add(fadeOut);
            _welcomeFadeOut.Begin();

            await tcs.Task;

            Visibility = Visibility.Collapsed;
            SyncPanel.Visibility = Visibility.Visible;
            SyncPanel.Opacity = 1;
            WelcomePanel.Opacity = 0;
        }
    }
}
