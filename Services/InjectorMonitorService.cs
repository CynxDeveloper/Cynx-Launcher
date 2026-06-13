using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SusanooLauncher.Services
{
    public sealed class InjectorDetectionResult
    {
        public int ProcessId { get; init; }
        public required string ProcessName { get; init; }
        public required string MatchedPattern { get; init; }
        public string Details { get; init; } = "Known suspicious process name detected.";
        public string Reason { get; init; } = "injector_process";
    }

    /// <summary>
    /// Scans for injector/cheat tools (process name, path, window title), closes them, and keeps the game running.
    /// </summary>
    internal sealed class InjectorMonitorService
    {
        public static InjectorMonitorService Instance { get; } = new();

        private static readonly string[] SuspiciousPatterns =
        {
            "injector",
            "xenos",
            "extremeinjector",
            "cheatengine",
            "cheat engine",
            "aimmy",
            "aimbot",
            "eulen",
            "kiddion",
            "wemod",
            "reclass",
            "kdmapper",
            "dllinject",
            "softaim",
            "triggerbot",
            "wallhack",
            "modmenu",
            "frida",
            "processhacker",
            "uuu",
            "unlocker",
            "universalue4",
            "ue4unlocker",
            "unrealengine4unlocker",
            "otis_inf",
            "otisinf",
        };

        private static readonly string[] SuspiciousWindowTitleHints =
        {
            "universal unreal engine",
            "ue4 unlocker",
            "universalue4",
            "injector",
            "cheat engine",
            "xenos",
            "extreme injector",
        };

        private static readonly string[] LauncherProcessNames =
        {
            "SusanooLauncher",
        };

        private static readonly string[] GameProcessNames =
        {
            "FortniteClient-Win64-Shipping",
            "Arc",
            "FortniteClient-Win64-Shipping_EAC",
            "FortniteClient-Win64-Shipping_BE",
            "FortniteLauncher",
        };

        private readonly object _gate = new();
        private CancellationTokenSource? _cts;
        private bool _handlingDetection;

        public event Action<InjectorDetectionResult>? SuspiciousProcessDetected;

        public bool IsMonitoring { get; private set; }

        public InjectorDetectionResult? ScanOnce()
        {
            foreach (Process process in Process.GetProcesses())
            {
                using (process)
                {
                    string name = process.ProcessName;
                    if (IsIgnoredProcess(name))
                        continue;

                    string? match = FindSuspiciousMatch(name);
                    if (match != null)
                    {
                        return BuildResult(process.Id, name, match, "Known suspicious process name detected.");
                    }

                    string? path = TryGetProcessPath(process);
                    if (path != null)
                    {
                        match = FindSuspiciousMatch(Path.GetFileNameWithoutExtension(path))
                            ?? FindSuspiciousMatch(path);
                        if (match != null)
                        {
                            return BuildResult(process.Id, name, match, "Known suspicious executable path detected.");
                        }
                    }
                }
            }

            return ScanSuspiciousWindows();
        }

        public void Start()
        {
            lock (_gate)
            {
                if (IsMonitoring)
                    return;

                _cts = new CancellationTokenSource();
                IsMonitoring = true;
                _ = RunMonitorLoopAsync(_cts.Token);
            }
        }

        public void Stop()
        {
            lock (_gate)
            {
                if (!IsMonitoring)
                    return;

                _cts?.Cancel();
                _cts?.Dispose();
                _cts = null;
                IsMonitoring = false;
                _handlingDetection = false;
            }
        }

        public static bool TryTerminateSuspiciousProcess(InjectorDetectionResult detection)
        {
            bool killed = false;

            try
            {
                using Process process = Process.GetProcessById(detection.ProcessId);
                if (!IsIgnoredProcess(process.ProcessName))
                {
                    process.Kill(entireProcessTree: true);
                    killed = true;
                }
            }
            catch
            {
                // Fall through to name / taskkill attempts.
            }

            if (!killed)
            {
                foreach (Process process in Process.GetProcessesByName(detection.ProcessName))
                {
                    try
                    {
                        if (IsIgnoredProcess(process.ProcessName))
                            continue;

                        process.Kill(entireProcessTree: true);
                        killed = true;
                    }
                    catch
                    {
                        // Try the next matching process.
                    }
                }
            }

            if (!killed)
                killed = TryForceKillProcessTree(detection.ProcessId);

            return killed;
        }

        public async Task<InjectorDetectionResult?> CloseAllSuspiciousProcessesAsync()
        {
            InjectorDetectionResult? firstClosed = null;

            for (int attempt = 0; attempt < 12; attempt++)
            {
                InjectorDetectionResult? hit = ScanOnce();
                if (hit == null)
                    break;

                TryTerminateSuspiciousProcess(hit);
                firstClosed ??= hit;
                await Task.Delay(400);
            }

            return firstClosed;
        }

        public void HandleDetection(InjectorDetectionResult detection)
        {
            lock (_gate)
            {
                if (_handlingDetection)
                    return;
                _handlingDetection = true;
            }

            for (int i = 0; i < 3; i++)
            {
                TryTerminateSuspiciousProcess(detection);
                if (ScanOnce() == null)
                    break;
                Thread.Sleep(250);
            }

            SuspiciousProcessDetected?.Invoke(detection);
            _ = ResetDetectionCooldownAsync();
        }

        public static bool IsGameRunning()
        {
            return Process.GetProcessesByName("FortniteClient-Win64-Shipping").Length > 0
                || Process.GetProcessesByName("Arc").Length > 0;
        }

        private async Task ResetDetectionCooldownAsync()
        {
            try
            {
                await Task.Delay(1500);
            }
            catch
            {
                return;
            }

            lock (_gate)
            {
                _handlingDetection = false;
            }
        }

        private async Task RunMonitorLoopAsync(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                try
                {
                    InjectorDetectionResult? hit = ScanOnce();
                    if (hit != null)
                        HandleDetection(hit);
                }
                catch
                {
                    // Ignore transient process access errors during scan.
                }

                try
                {
                    int delayMs = IsGameRunning() ? 1000 : 2000;
                    await Task.Delay(delayMs, token);
                }
                catch (TaskCanceledException)
                {
                    break;
                }
            }
        }

        private static InjectorDetectionResult BuildResult(
            int processId,
            string processName,
            string match,
            string details)
        {
            return new InjectorDetectionResult
            {
                ProcessId = processId,
                ProcessName = processName,
                MatchedPattern = match,
                Details = details,
            };
        }

        private static InjectorDetectionResult? ScanSuspiciousWindows()
        {
            InjectorDetectionResult? found = null;

            EnumWindows((hWnd, _) =>
            {
                if (!IsWindowVisible(hWnd))
                    return true;

                var title = new StringBuilder(512);
                if (GetWindowText(hWnd, title, title.Capacity) <= 0)
                    return true;

                string windowTitle = title.ToString();
                if (string.IsNullOrWhiteSpace(windowTitle))
                    return true;

                string? match = FindSuspiciousWindowMatch(windowTitle);
                if (match == null)
                    return true;

                GetWindowThreadProcessId(hWnd, out uint pid);
                if (pid == 0)
                    return true;

                try
                {
                    using Process process = Process.GetProcessById((int)pid);
                    if (IsIgnoredProcess(process.ProcessName))
                        return true;

                    found = BuildResult(
                        process.Id,
                        process.ProcessName,
                        match,
                        $"Known suspicious window detected: \"{windowTitle}\"");
                }
                catch
                {
                    return true;
                }

                return false;
            }, IntPtr.Zero);

            return found;
        }

        private static string? TryGetProcessPath(Process process)
        {
            try
            {
                return process.MainModule?.FileName;
            }
            catch
            {
                try
                {
                    return process.StartInfo?.FileName;
                }
                catch
                {
                    return null;
                }
            }
        }

        private static bool TryForceKillProcessTree(int processId)
        {
            try
            {
                using Process killer = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "taskkill",
                        Arguments = $"/F /PID {processId} /T",
                        CreateNoWindow = true,
                        UseShellExecute = false,
                    },
                };
                killer.Start();
                killer.WaitForExit(4000);
                return killer.ExitCode == 0;
            }
            catch
            {
                return false;
            }
        }

        private static bool IsIgnoredProcess(string processName)
        {
            if (LauncherProcessNames.Any(p =>
                    processName.Equals(p, StringComparison.OrdinalIgnoreCase)))
                return true;

            if (GameProcessNames.Any(p =>
                    processName.Equals(p, StringComparison.OrdinalIgnoreCase)))
                return true;

            return processName.Contains("devenv", StringComparison.OrdinalIgnoreCase)
                || processName.Contains("cursor", StringComparison.OrdinalIgnoreCase)
                || processName.Equals("Code", StringComparison.OrdinalIgnoreCase);
        }

        private static string? FindSuspiciousMatch(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return null;

            string normalized = value.Replace(" ", "", StringComparison.OrdinalIgnoreCase);

            foreach (string pattern in SuspiciousPatterns)
            {
                string key = pattern.Replace(" ", "", StringComparison.OrdinalIgnoreCase);
                if (normalized.Contains(key, StringComparison.OrdinalIgnoreCase))
                    return pattern;
            }

            return null;
        }

        private static string? FindSuspiciousWindowMatch(string windowTitle)
        {
            foreach (string hint in SuspiciousWindowTitleHints)
            {
                if (windowTitle.Contains(hint, StringComparison.OrdinalIgnoreCase))
                    return hint;
            }

            return FindSuspiciousMatch(windowTitle);
        }

        private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

        [DllImport("user32.dll")]
        private static extern bool EnumWindows(EnumWindowsProc lpEnum, IntPtr lParam);

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        private static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

        [DllImport("user32.dll")]
        private static extern bool IsWindowVisible(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);
    }
}
