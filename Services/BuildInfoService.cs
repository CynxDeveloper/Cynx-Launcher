using System;
using System.IO;
using System.Text.RegularExpressions;
using System.Windows.Media.Imaging;

namespace SusanooLauncher.Services
{
    internal sealed class BuildInfo
    {
        public string BuildPath { get; init; } = "";
        public string DisplayName { get; init; } = "No build selected";
        public string? SplashPath { get; init; }
        public bool IsValid { get; init; }
    }

    internal static class BuildInfoService
    {
        private static readonly string[] SplashRelativePaths =
        {
            "splash.png",
            "Splash.png",
            "data\\splash.png",
            "Data\\splash.png",
            "FortniteGame\\Content\\Splash\\Splash.png",
            "FortniteGame\\Content\\Splash\\splash.png",
            "FortniteGame\\Content\\Splash\\Splash.bmp",
        };

        public static BuildInfo Resolve(string? buildPath)
        {
            if (string.IsNullOrWhiteSpace(buildPath) || !Directory.Exists(buildPath))
            {
                return new BuildInfo
                {
                    DisplayName = "No build selected",
                    IsValid = false,
                };
            }

            string normalized = Path.GetFullPath(buildPath);
            string shipping = Path.Combine(
                normalized,
                GameVersionConstants.ShippingExeRelative.Replace('\\', Path.DirectorySeparatorChar));

            return new BuildInfo
            {
                BuildPath = normalized,
                DisplayName = FormatBuildDisplayName(normalized),
                SplashPath = FindSplashImage(normalized),
                IsValid = File.Exists(shipping),
            };
        }

        public static BitmapImage? LoadSplashImage(string? splashPath)
        {
            if (string.IsNullOrWhiteSpace(splashPath) || !File.Exists(splashPath))
                return null;

            try
            {
                var image = new BitmapImage();
                image.BeginInit();
                image.CacheOption = BitmapCacheOption.OnLoad;
                image.UriSource = new Uri(splashPath, UriKind.Absolute);
                image.EndInit();
                image.Freeze();
                return image;
            }
            catch
            {
                return null;
            }
        }

        private static string? FindSplashImage(string buildPath)
        {
            foreach (string relative in SplashRelativePaths)
            {
                string candidate = Path.Combine(buildPath, relative);
                if (File.Exists(candidate))
                    return candidate;
            }

            return null;
        }

        private static string FormatBuildDisplayName(string buildPath)
        {
            string folder = Path.GetFileName(buildPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
            if (string.IsNullOrWhiteSpace(folder))
                return "Fortnite Build";

            string name = folder.Replace("++", "", StringComparison.Ordinal).Replace("+", " ").Trim();

            if (name.StartsWith("Fortnite ", StringComparison.OrdinalIgnoreCase))
                name = name["Fortnite ".Length..].Trim();

            int clIndex = name.IndexOf("-CL-", StringComparison.OrdinalIgnoreCase);
            if (clIndex > 0)
                name = name[..clIndex].Trim();

            int windowsIndex = name.IndexOf("-Windows", StringComparison.OrdinalIgnoreCase);
            if (windowsIndex > 0)
                name = name[..windowsIndex].Trim();

            Match version = Regex.Match(name, @"\d+\.\d+");
            if (version.Success)
            {
                if (version.Value == "19.10")
                    return $"Fortnite 19.10 ({GameVersionConstants.SeasonLabel})";
                if (name.Contains("Release", StringComparison.OrdinalIgnoreCase))
                    return $"Fortnite {version.Value}";
            }

            return string.IsNullOrWhiteSpace(name) ? folder : name;
        }
    }
}
