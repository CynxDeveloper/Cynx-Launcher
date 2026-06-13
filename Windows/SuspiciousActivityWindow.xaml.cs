using System.Windows;
using SusanooLauncher.Services;

namespace SusanooLauncher.Windows
{
    public partial class SuspiciousActivityWindow : Wpf.Ui.Controls.FluentWindow
    {
        public SuspiciousActivityWindow(InjectorDetectionResult detection)
        {
            InitializeComponent();

            DetailsText.Text = $"Details: {detection.Details}";
            ReasonText.Text = $"Reason: {detection.Reason}";
        }

        private void CloseClicked(object sender, RoutedEventArgs e) => Close();

        public static void Show(InjectorDetectionResult detection)
        {
            var window = new SuspiciousActivityWindow(detection);
            window.ShowDialog();
        }
    }
}
