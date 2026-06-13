using System.Windows;
using SusanooLauncher.Windows;

namespace SusanooLauncher.Services
{
    internal static class AntiCheatUiService
    {
        private static readonly object Gate = new();
        private static bool _dialogShown;

        public static void ShowSuspiciousActivity(InjectorDetectionResult detection)
        {
            lock (Gate)
            {
                if (_dialogShown)
                    return;
                _dialogShown = true;
            }

            void Show()
            {
                SuspiciousActivityWindow.Show(detection);
            }

            if (Application.Current?.Dispatcher?.CheckAccess() == true)
                Show();
            else
                Application.Current?.Dispatcher?.Invoke(Show);
        }

        public static void WireInjectorMonitor()
        {
            InjectorMonitorService.Instance.SuspiciousProcessDetected += detection =>
            {
                ShowSuspiciousActivity(detection);
            };
        }
    }
}
