using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;

namespace SusanooLauncher.Pages
{
    public partial class SupportPage : Page
    {
        public SupportPage()
        {
            InitializeComponent();
        }

        private void OpenDiscordClicked(object sender, RoutedEventArgs e)
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = "https://discord.gg/qNXVhkDwfH",
                UseShellExecute = true
            });
        }
    }
}

