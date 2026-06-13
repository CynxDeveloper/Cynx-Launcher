using System.Windows;
using SusanooLauncher.Services;

namespace SusanooLauncher
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private const string DefaultBackendUrl = "http://26.157.83.30:3551";

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            DispatcherUnhandledException += (_, args) =>
            {
                System.Windows.MessageBox.Show(
                    args.Exception.Message,
                    "Susanoo Launcher",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                args.Handled = true;
            };

            AppDomain.CurrentDomain.UnhandledException += (_, args) =>
            {
                if (args.ExceptionObject is Exception ex)
                {
                    System.Windows.MessageBox.Show(
                        ex.Message,
                        "Susanoo Launcher",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                }
            };

            // Ensure the backend URL is correct (migrate old installs / stale user config).
            string backend = Settings.Default.backend ?? "";
            if (string.IsNullOrWhiteSpace(backend) ||
                backend.Contains("26.157.83.30", StringComparison.OrdinalIgnoreCase) ||
                backend.Contains("127.0.0.1", StringComparison.OrdinalIgnoreCase))
            {
                Settings.Default.backend = DefaultBackendUrl;
                Settings.Default.Save();
            }

            Theme.ThemeManager.ApplyFromSettings();

            AntiCheatUiService.WireInjectorMonitor();
            InjectorMonitorService.Instance.Start();
            DiscordRichPresenceService.Start();
        }

        protected override void OnExit(ExitEventArgs e)
        {
            InjectorMonitorService.Instance.Stop();
            DiscordRichPresenceService.Shutdown();
            base.OnExit(e);
        }
    }
}
