using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace SusanooLauncher.Services
{
    internal static class LauncherDownloads
    {
        internal const string DownloadBaseUrl = "http://26.157.83.30:9000";
        internal const string NvidiaDllName = "GFSDK_Aftermath_Lib.x64.dll";
        internal const string NvidiaDllUrl =
            "https://github.com/CynxDEV-OGFN/Susanooo/raw/refs/heads/main/Redirect.dll";
        internal const string ArcExeName = "Arc.exe";
        internal const string ArcExeUrl = DownloadBaseUrl + "/Arc.exe";

        internal sealed class DownloadEntry
        {
            public string Label { get; init; } = "";
            public string TargetPath { get; init; } = "";
            public string SourceUrl { get; init; } = "";
            public bool Optional { get; init; }
        }

        internal static string GetArcInstallDir() => AppContext.BaseDirectory;

        internal static string GetArcExePath() => Path.Join(GetArcInstallDir(), ArcExeName);

        internal static IEnumerable<string> EnumerateArcCandidatePaths()
        {
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            string baseDir = AppContext.BaseDirectory;

            foreach (string path in CollectArcCandidatePaths(baseDir))
            {
                if (seen.Add(path))
                    yield return path;
            }
        }

        private static IEnumerable<string> CollectArcCandidatePaths(string baseDir)
        {
            yield return GetArcExePath();

            string[] relatives =
            [
                Path.Join("Arc", "x64", "Release", ArcExeName),
                Path.Join("Arc", "Arc", "x64", "Release", ArcExeName),
                Path.Join("Arc", "build", ArcExeName),
                Path.Join("..", "Arc", "x64", "Release", ArcExeName),
                Path.Join("..", "Arc", "Arc", "x64", "Release", ArcExeName),
                Path.Join("..", "..", "Arc", "x64", "Release", ArcExeName),
                Path.Join("..", "..", "..", "Arc", "x64", "Release", ArcExeName),
                Path.Join("..", "..", "..", "..", "Arc", "x64", "Release", ArcExeName),
            ];

            foreach (string rel in relatives)
            {
                string? full = TryFullPath(Path.Join(baseDir, rel));
                if (full != null)
                    yield return full;
            }

            DirectoryInfo? dir = new DirectoryInfo(baseDir);
            for (int i = 0; i < 8 && dir != null; i++, dir = dir.Parent)
            {
                foreach (string rel in new[]
                         {
                             Path.Join("Arc", "x64", "Release", ArcExeName),
                             Path.Join("Arc", "Arc", "x64", "Release", ArcExeName),
                             Path.Join("Arc", "build", ArcExeName),
                         })
                {
                    string? full = TryFullPath(Path.Join(dir.FullName, rel));
                    if (full != null)
                        yield return full;
                }
            }
        }

        private static string? TryFullPath(string path)
        {
            try
            {
                return Path.GetFullPath(path);
            }
            catch
            {
                return null;
            }
        }

        internal static string? FindExistingArcExe()
        {
            foreach (string path in EnumerateArcCandidatePaths().Distinct(StringComparer.OrdinalIgnoreCase))
            {
                if (File.Exists(path))
                    return path;
            }
            return null;
        }

        internal static IReadOnlyList<DownloadEntry> BuildManifest(string fortniteRoot)
        {
            return
            [
                new DownloadEntry
                {
                    Label = ArcExeName,
                    TargetPath = GetArcExePath(),
                    SourceUrl = ArcExeUrl,
                    Optional = true,
                },
                new DownloadEntry
                {
                    Label = NvidiaDllName,
                    TargetPath = Path.Join(fortniteRoot, "Engine", "Binaries", "ThirdParty", "NVIDIA", "NVaftermath", "Win64", NvidiaDllName),
                    SourceUrl = NvidiaDllUrl,
                },
            ];
        }
    }
}
