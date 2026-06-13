using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace SusanooLauncher.Services
{
    internal sealed class FortniteLaunchService
    {
        private static readonly HttpClient _http = CreateHttpClient();

        private const string EpicOAuthClientId = "ec684b8c687f479fadea3e7aad8cfcfd";
        private const string EpicOAuthClientSecret = "da461bc2e0404458d5b4107474df7aad";

        internal string? LastError { get; private set; }

        private static HttpClient CreateHttpClient()
        {
            var client = new HttpClient();
            client.Timeout = TimeSpan.FromMinutes(30);
            return client;
        }

        private static void ReportStatus(Action<string> status, string text)
        {
            try
            {
                status(text);
            }
            catch
            {
                // UI may be gone if the user closed the launcher mid-download.
            }
        }

        private sealed class OAuthTokenResponse
        {
            [JsonPropertyName("access_token")]
            public string? AccessToken { get; set; }

            [JsonPropertyName("error")]
            public string? Error { get; set; }

            [JsonPropertyName("error_description")]
            public string? ErrorDescription { get; set; }
        }

        private static async Task TryDelayAsync(int i) => await Task.Delay(200 * (i + 1));

        private static async Task TryDeleteAsync(string path)
        {
            for (int i = 0; i < 8; i++)
            {
                try
                {
                    if (File.Exists(path))
                        File.Delete(path);
                    return;
                }
                catch (IOException) { await TryDelayAsync(i); }
                catch (UnauthorizedAccessException) { await TryDelayAsync(i); }
            }
        }

        private static async Task<bool> TryMoveOrCopyReplaceAsync(string src, string dst)
        {
            for (int i = 0; i < 14; i++)
            {
                try
                {
                    if (File.Exists(dst))
                        File.Delete(dst);
                    File.Move(src, dst);
                    return true;
                }
                catch (IOException)
                {
                    try
                    {
                        File.Copy(src, dst, overwrite: true);
                        return true;
                    }
                    catch { }

                    await TryDelayAsync(i);
                }
                catch (UnauthorizedAccessException)
                {
                    try
                    {
                        File.Copy(src, dst, overwrite: true);
                        return true;
                    }
                    catch { }

                    await TryDelayAsync(i);
                }
            }
            return false;
        }

        private static string FormatEta(double seconds)
        {
            if (seconds < 1) return "0s";
            TimeSpan t = TimeSpan.FromSeconds(seconds);
            if (t.TotalHours >= 1) return $"{(int)t.TotalHours}h {t.Minutes}m";
            if (t.TotalMinutes >= 1) return $"{t.Minutes}m {t.Seconds}s";
            return $"{t.Seconds}s";
        }

        private async Task<bool> DownloadFileAsync(string url, string destPath, string label, Action<string> status, bool optional = false)
        {
            string tempPath = Path.Join(Path.GetTempPath(), $"Susanoo_{Guid.NewGuid():N}_{Path.GetFileName(destPath)}.download");

            try
            {
                if (File.Exists(destPath))
                    return true;

                if (!Uri.TryCreate(url, UriKind.Absolute, out Uri? uri) ||
                    (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
                {
                    if (!optional)
                        LastError = $"Invalid download URL for {label}:\n{url}";
                    return false;
                }

                ReportStatus(status, $"Downloading {label}...");

                await TryDeleteAsync(tempPath);

                using HttpResponseMessage resp = await _http.GetAsync(uri, HttpCompletionOption.ResponseHeadersRead);
                if (!resp.IsSuccessStatusCode)
                {
                    if (!optional)
                        LastError = $"Failed downloading {label}\nURL: {url}\n\nHTTP {(int)resp.StatusCode} {resp.ReasonPhrase}";
                    return false;
                }

                string? dir = Path.GetDirectoryName(destPath);
                if (!string.IsNullOrEmpty(dir))
                    Directory.CreateDirectory(dir);

                long? total = resp.Content.Headers.ContentLength;
                await using Stream src = await resp.Content.ReadAsStreamAsync();
                await using FileStream dst = new FileStream(tempPath, FileMode.Create, FileAccess.Write, FileShare.ReadWrite);

                byte[] buffer = new byte[1024 * 128];
                long readTotal = 0;
                var sw = Stopwatch.StartNew();
                long lastUiMs = 0;

                while (true)
                {
                    int read = await src.ReadAsync(buffer.AsMemory(0, buffer.Length));
                    if (read <= 0) break;
                    await dst.WriteAsync(buffer.AsMemory(0, read));
                    readTotal += read;

                    long ms = sw.ElapsedMilliseconds;
                    if (ms - lastUiMs >= 300)
                    {
                        lastUiMs = ms;
                        string progressText;
                        if (total.HasValue && total.Value > 0)
                        {
                            double elapsedSec = Math.Max(sw.Elapsed.TotalSeconds, 0.001);
                            double speedBytesPerSec = readTotal / elapsedSec;
                            double remainingBytes = Math.Max(total.Value - readTotal, 0);
                            string eta = speedBytesPerSec > 1 ? FormatEta(remainingBytes / speedBytesPerSec) : "--";
                            progressText = $"Downloading {label}... {(readTotal / 1024d / 1024d):0.0}/{(total.Value / 1024d / 1024d):0.0} MB ({(readTotal * 100d / total.Value):0.0}%) - ETA {eta}";
                        }
                        else
                        {
                            progressText = $"Downloading {label}... {(readTotal / 1024d / 1024d):0.0} MB";
                        }
                        ReportStatus(status, progressText);
                    }
                }

                bool placed = await TryMoveOrCopyReplaceAsync(tempPath, destPath);
                if (!placed)
                    throw new IOException("Failed to place downloaded file (file locked).");

                await TryDeleteAsync(tempPath);
                ReportStatus(status, $"Downloaded {label}");
                return true;
            }
            catch (Exception ex)
            {
                await TryDeleteAsync(tempPath);
                if (!optional)
                    LastError = $"Failed downloading {label}\nURL: {url}\n\n{ex.GetType().Name}: {ex.Message}";
                return false;
            }
        }

        private async Task<bool> EnsureNvidiaLibsAsync(string basePath, Action<string> status)
        {
            string destPath = Path.Join(basePath, "Engine", "Binaries", "ThirdParty", "NVIDIA", "NVaftermath", "Win64", LauncherDownloads.NvidiaDllName);
            return await DownloadFileAsync(LauncherDownloads.NvidiaDllUrl, destPath, LauncherDownloads.NvidiaDllName, status);
        }

        private async Task<string?> EnsureArcClientAsync(Action<string> status)
        {
            string installPath = LauncherDownloads.GetArcExePath();
            ArcLaunchHelper.DeployBuiltArc(installPath);

            if (File.Exists(installPath))
                return installPath;

            ReportStatus(status, "Checking Arc client...");
            string? downloadError = LastError;
            LastError = null;
            bool downloaded = await DownloadFileAsync(
                LauncherDownloads.ArcExeUrl,
                installPath,
                LauncherDownloads.ArcExeName,
                status,
                optional: true);
            if (downloaded && File.Exists(installPath))
                return installPath;

            string? existing = LauncherDownloads.FindExistingArcExe();
            if (existing == null)
            {
                string hint =
                    "Arc.exe is required to launch Fortnite 19.10.\n\n" +
                    "Build Arc: open Arc\\Arc.vcxproj (Release | x64), rebuild the launcher, or copy Arc.exe to:\n" +
                    installPath;
                if (!string.IsNullOrWhiteSpace(downloadError))
                    hint = downloadError + "\n\n" + hint;
                LastError ??= hint;
                return null;
            }

            try
            {
                ReportStatus(status, "Installing Arc client...");
                Directory.CreateDirectory(LauncherDownloads.GetArcInstallDir());
                File.Copy(existing, installPath, overwrite: true);
                return installPath;
            }
            catch (Exception ex)
            {
                LastError = $"Failed to install Arc.exe from:\n{existing}\n\n{ex.Message}";
                return null;
            }
        }

        private static void WriteArcConfig(string arcDir, string shippingExePath)
        {
            var config = new
            {
                ClientID = EpicOAuthClientId,
                Executable = shippingExePath.Replace('/', '\\'),
                CL = 18734220,
            };

            string json = JsonSerializer.Serialize(config, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(Path.Join(arcDir, "Config.json"), json);
        }

        private static string? ResolveStoredAccessToken()
        {
            string token = (Settings.Default.accessToken ?? "").Trim();
            if (string.IsNullOrWhiteSpace(token))
                return null;

            return token.StartsWith("eg1~", StringComparison.OrdinalIgnoreCase)
                ? token[4..]
                : token;
        }

        private async Task<string?> FetchAccessTokenAsync(string backendUrl, string authLogin, string authPassword)
        {
            try
            {
                string tokenUrl = backendUrl.TrimEnd('/') + "/account/api/oauth/token";
                string basic = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{EpicOAuthClientId}:{EpicOAuthClientSecret}"));

                using var req = new HttpRequestMessage(HttpMethod.Post, tokenUrl);
                req.Headers.Authorization = new AuthenticationHeaderValue("Basic", basic);
                req.Content = new StringContent(
                    $"grant_type=password&username={Uri.EscapeDataString(authLogin)}&password={Uri.EscapeDataString(authPassword)}",
                    Encoding.UTF8,
                    "application/x-www-form-urlencoded");

                using HttpResponseMessage resp = await _http.SendAsync(req);
                string body = await resp.Content.ReadAsStringAsync();

                OAuthTokenResponse? token;
                try
                {
                    token = JsonSerializer.Deserialize<OAuthTokenResponse>(body);
                }
                catch
                {
                    LastError = "Failed to parse authentication response from the backend.";
                    return null;
                }

                if (!resp.IsSuccessStatusCode || string.IsNullOrWhiteSpace(token?.AccessToken))
                {
                    string detail = token?.ErrorDescription ?? token?.Error ?? body;
                    LastError = $"Login failed.\n\n{detail}";
                    return null;
                }

                return token.AccessToken;
            }
            catch (Exception ex)
            {
                LastError = $"Could not reach the backend.\n\n{ex.Message}";
                return null;
            }
        }

        internal async Task<bool> LaunchAsync(
            string basePath,
            string authLogin,
            string authPassword,
            string authType,
            string backendUrl,
            Action<string> status)
        {
            LastError = null;

            try
            {
                if (string.IsNullOrWhiteSpace(basePath))
                {
                    LastError = "No Fortnite folder selected. Choose your build folder in Library first.";
                    return false;
                }

                string shippingPath = Path.Join(basePath, GameVersionConstants.ShippingExeRelative.Replace('\\', Path.DirectorySeparatorChar));
                if (!File.Exists(shippingPath))
                {
                    LastError = "Fortnite not found. Please select the correct Fortnite folder.";
                    return false;
                }

                (bool banned, bool ipBanned, string banMsg) = await BanCheckService.CheckEmailAsync(authLogin);
                if (banned)
                {
                    LastError = banMsg;
                    if (ipBanned)
                    {
                        System.Windows.MessageBox.Show(
                            banMsg,
                            "Susanoo Launcher",
                            System.Windows.MessageBoxButton.OK,
                            System.Windows.MessageBoxImage.Warning);
                    }
                    return false;
                }

                ReportStatus(status, "Scanning for cheat tools...");
                InjectorDetectionResult? preLaunchClosed =
                    await InjectorMonitorService.Instance.CloseAllSuspiciousProcessesAsync();
                if (preLaunchClosed != null)
                    AntiCheatUiService.ShowSuspiciousActivity(preLaunchClosed);

                ReportStatus(status, "Authenticating...");
                string? accessToken = ResolveStoredAccessToken();
                if (accessToken == null)
                    accessToken = await FetchAccessTokenAsync(backendUrl, authLogin, authPassword);
                if (accessToken == null)
                    return false;

                foreach (string name in new[]
                         {
                             "FortniteClient-Win64-Shipping",
                             "FortniteClient-Win64-Shipping_EAC",
                             "FortniteClient-Win64-Shipping_BE",
                             "FortniteLauncher",
                             "Arc",
                             "EpicGamesLauncher",
                             "EpicWebHelper",
                         })
                {
                    foreach (Process p in Process.GetProcessesByName(name))
                    {
                        try { p.Kill(); } catch { }
                    }
                }

                await Task.Delay(500);

                ReportStatus(status, "Preparing Starfall...");
                if (!StarfallDeployService.TryDeploy(basePath))
                {
                    ReportStatus(status, "Checking NVIDIA libs...");
                    if (!await EnsureNvidiaLibsAsync(basePath, status))
                        return false;
                }

                string shippingArgs = PlooshiLaunchArgs.BuildShippingArguments(
                    authLogin, authPassword, authType, backendUrl);
                string helperArgs = PlooshiLaunchArgs.BuildHelperArguments(backendUrl);

                ReportStatus(status, "Starting Fortnite...");
                Process? game = Proc.Start(
                    basePath,
                    GameVersionConstants.ShippingExeRelative,
                    shippingArgs,
                    suspend: false,
                    workingDirectory: Path.GetDirectoryName(shippingPath));

                if (game == null)
                {
                    LastError = "Failed to start FortniteClient-Win64-Shipping.exe\n\n" + shippingPath;
                    return false;
                }

                ReportStatus(status, "Starting EAC...");
                Process? eacProc = Proc.Start(basePath, GameVersionConstants.EacExeRelative, helperArgs, suspend: true);

                ReportStatus(status, "Starting Fortnite launcher...");
                Process? launcherProc = Proc.Start(basePath, GameVersionConstants.LauncherExeRelative, helperArgs, suspend: true);

                ReportStatus(status, "Loading...");
                await Task.Run(() =>
                {
                    try { game.WaitForInputIdle(); } catch { }
                });

                ReportStatus(status, "Running");
                DiscordRichPresenceService.Update(state: "In Game");
                LocalFeatureStore.StartPlaySession();
                LocalFeatureStore.UnlockAchievement("first_launch");
                InjectorMonitorService.Instance.Start();

                try
                {
                    await game.WaitForExitAsync();

                    while (InjectorMonitorService.IsGameRunning())
                        await Task.Delay(2000);
                }
                finally
                {
                    LocalFeatureStore.EndPlaySession();
                }

                try { eacProc?.Kill(); } catch { }
                try { launcherProc?.Kill(); } catch { }

                DiscordRichPresenceService.Update(state: "");
                ReportStatus(status, "Ready");
                return true;
            }
            catch (Exception ex)
            {
                LastError = $"Launch failed unexpectedly.\n\n{ex.Message}";
                return false;
            }
        }
    }
}
