using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Reflection;
using System.Text.Json;
using System.Windows;

namespace SusanooLauncher.Services
{
    internal sealed class UpdateInfo
    {
        public bool UpdateAvailable { get; init; }
        public string CurrentVersion { get; init; } = "1.0.0";
        public string LatestVersion { get; init; } = "";
        public string? DownloadUrl { get; init; }
        public string ReleaseNotes { get; init; } = "";
    }

    internal static class LauncherUpdateService
    {
        private static string CurrentVersion =>
            Assembly.GetExecutingAssembly().GetName().Version?.ToString(3) ?? "1.0.0";

        public static async Task<UpdateInfo> CheckAsync()
        {
            try
            {
                string url = BackendApiClient.Url("/phoenix/api/launcher/update");
                using HttpResponseMessage resp = await BackendApiClient.Http.GetAsync(url);
                string json = await resp.Content.ReadAsStringAsync();
                using JsonDocument doc = JsonDocument.Parse(json);
                JsonElement root = doc.RootElement;

                return new UpdateInfo
                {
                    UpdateAvailable = root.TryGetProperty("updateAvailable", out JsonElement u) &&
                                      u.ValueKind == JsonValueKind.True,
                    CurrentVersion = root.TryGetProperty("currentVersion", out JsonElement c)
                        ? c.GetString() ?? CurrentVersion
                        : CurrentVersion,
                    LatestVersion = root.TryGetProperty("latestVersion", out JsonElement l)
                        ? l.GetString() ?? ""
                        : "",
                    DownloadUrl = root.TryGetProperty("downloadUrl", out JsonElement d)
                        ? d.GetString()
                        : null,
                    ReleaseNotes = root.TryGetProperty("releaseNotes", out JsonElement n)
                        ? n.GetString() ?? ""
                        : "",
                };
            }
            catch
            {
                return new UpdateInfo { CurrentVersion = CurrentVersion };
            }
        }

        public static async Task PromptAndApplyIfAvailableAsync(Window? owner)
        {
            UpdateInfo info = await CheckAsync();
            if (!info.UpdateAvailable || string.IsNullOrWhiteSpace(info.DownloadUrl))
                return;

            string notes = string.IsNullOrWhiteSpace(info.ReleaseNotes)
                ? ""
                : $"\n\n{info.ReleaseNotes}";

            MessageBoxResult choice = MessageBox.Show(
                owner,
                $"A new Susanoo Launcher is available.\n\nCurrent: {info.CurrentVersion}\nLatest: {info.LatestVersion}{notes}\n\nDownload and run the installer now?",
                "Susanoo Update",
                MessageBoxButton.YesNo,
                MessageBoxImage.Information);

            if (choice != MessageBoxResult.Yes)
                return;

            await DownloadAndRunInstallerAsync(info.DownloadUrl!);
        }

        private static async Task DownloadAndRunInstallerAsync(string downloadUrl)
        {
            string dir = Path.Combine(Path.GetTempPath(), "SusanooLauncher_Update");
            Directory.CreateDirectory(dir);
            string dest = Path.Combine(dir, "setup.exe");

            using HttpResponseMessage resp = await BackendApiClient.Http.GetAsync(downloadUrl);
            resp.EnsureSuccessStatusCode();
            await using Stream stream = await resp.Content.ReadAsStreamAsync();
            await using FileStream file = File.Create(dest);
            await stream.CopyToAsync(file);

            Process.Start(new ProcessStartInfo
            {
                FileName = dest,
                UseShellExecute = true,
            });

            Application.Current.Shutdown();
        }
    }
}
