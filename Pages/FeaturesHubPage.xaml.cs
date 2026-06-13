using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using SusanooLauncher.Services;

namespace SusanooLauncher.Pages
{
    public partial class FeaturesHubPage : Page
    {
        public FeaturesHubPage()
        {
            InitializeComponent();
            Loaded += (_, __) => BuildUi();
        }

        private void BuildUi()
        {
            FeatureCategoriesPanel.Children.Clear();

            foreach (var group in FeatureRegistry.All.GroupBy(f => f.Category).OrderBy(g => g.Key))
            {
                FeatureCategoriesPanel.Children.Add(new TextBlock
                {
                    Text = group.Key,
                    Foreground = new SolidColorBrush(Color.FromArgb(160, 255, 255, 255)),
                    FontSize = 12,
                    FontWeight = FontWeights.SemiBold,
                    Margin = new Thickness(0, 16, 0, 8),
                });

                var wrap = new WrapPanel();
                foreach (LauncherFeature feature in group)
                    wrap.Children.Add(CreateFeatureCard(feature));

                FeatureCategoriesPanel.Children.Add(wrap);
            }
        }

        private static Border CreateFeatureCard(LauncherFeature feature)
        {
            bool canOpen = feature.Status != FeatureStatus.Planned && !string.IsNullOrWhiteSpace(feature.NavigateKey);

            var card = new Border
            {
                Width = 220,
                Margin = new Thickness(0, 0, 10, 10),
                CornerRadius = new CornerRadius(12),
                Background = new SolidColorBrush(Color.FromRgb(26, 31, 46)),
                BorderBrush = new SolidColorBrush(Color.FromRgb(42, 49, 69)),
                BorderThickness = new Thickness(1),
                Padding = new Thickness(12),
                Cursor = canOpen ? System.Windows.Input.Cursors.Hand : System.Windows.Input.Cursors.Arrow,
            };

            var stack = new StackPanel();
            stack.Children.Add(new TextBlock
            {
                Text = feature.Name,
                Foreground = Brushes.White,
                FontWeight = FontWeights.SemiBold,
                FontSize = 13,
                TextWrapping = TextWrapping.Wrap,
            });
            stack.Children.Add(new TextBlock
            {
                Text = StatusLabel(feature.Status),
                Foreground = StatusBrush(feature.Status),
                FontSize = 11,
                Margin = new Thickness(0, 4, 0, 0),
            });
            stack.Children.Add(new TextBlock
            {
                Text = feature.Description,
                Foreground = new SolidColorBrush(Color.FromArgb(180, 255, 255, 255)),
                FontSize = 11,
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 6, 0, 0),
            });
            card.Child = stack;

            if (canOpen)
                card.MouseLeftButtonUp += (_, __) => FeatureNavigation.Go(feature.NavigateKey!);

            return card;
        }

        private static string StatusLabel(FeatureStatus s) => s switch
        {
            FeatureStatus.Live => "● Live",
            FeatureStatus.Beta => "● Beta",
            _ => "○ Planned",
        };

        private static Brush StatusBrush(FeatureStatus s) => s switch
        {
            FeatureStatus.Live => new SolidColorBrush(Color.FromRgb(34, 197, 94)),
            FeatureStatus.Beta => new SolidColorBrush(Color.FromRgb(250, 204, 21)),
            _ => new SolidColorBrush(Color.FromArgb(140, 255, 255, 255)),
        };
    }
}
