using SusanooLauncher.Controls;

namespace SusanooLauncher.Theme
{
    internal static class ThemeManager
    {
        internal static LiveBackground? Background { get; set; }

        internal static void ApplyFromSettings()
        {
            if (Background == null)
                return;

            bool enabled = Settings.Default.liveBackground;
            Background.UseSquares = Settings.Default.liveBackgroundSquares;
            Background.IsActive = enabled;
        }
    }
}
