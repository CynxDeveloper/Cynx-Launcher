using System.IO;
using System.Text.Json;

namespace SusanooLauncher.Services
{
    internal sealed class LauncherPrefs
    {
        public List<string> WishlistOfferIds { get; set; } = [];
        public List<BuildSlot> BuildSlots { get; set; } = [];
        public string SupportACreator { get; set; } = "";
        public bool StreamerMode { get; set; }
        public bool HighContrast { get; set; }
        public long TotalPlayTimeSeconds { get; set; }
        public DateTime? SessionStartedUtc { get; set; }
        public Dictionary<string, bool> Achievements { get; set; } = new();
    }

    internal sealed class BuildSlot
    {
        public string Name { get; set; } = "";
        public string Path { get; set; } = "";
    }

    internal static class LocalFeatureStore
    {
        private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };
        private static LauncherPrefs? _cache;

        private static string PrefsPath =>
            Path.Join(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "SusanooLauncher", "prefs.json");

        public static LauncherPrefs Prefs
        {
            get
            {
                if (_cache != null)
                    return _cache;

                try
                {
                    if (File.Exists(PrefsPath))
                    {
                        _cache = JsonSerializer.Deserialize<LauncherPrefs>(File.ReadAllText(PrefsPath))
                            ?? new LauncherPrefs();
                        Normalize(_cache);
                        return _cache;
                    }
                }
                catch { }

                _cache = new LauncherPrefs();
                return _cache;
            }
        }

        public static void Save()
        {
            try
            {
                string dir = Path.GetDirectoryName(PrefsPath)!;
                Directory.CreateDirectory(dir);
                File.WriteAllText(PrefsPath, JsonSerializer.Serialize(Prefs, JsonOptions));
            }
            catch { }
        }

        public static void StartPlaySession()
        {
            Prefs.SessionStartedUtc = DateTime.UtcNow;
            Save();
        }

        public static void EndPlaySession()
        {
            if (Prefs.SessionStartedUtc == null)
                return;

            Prefs.TotalPlayTimeSeconds += (long)(DateTime.UtcNow - Prefs.SessionStartedUtc.Value).TotalSeconds;
            Prefs.SessionStartedUtc = null;
            Save();
        }

        public static void UnlockAchievement(string id)
        {
            Prefs.Achievements[id] = true;
            Save();
        }

        private static void Normalize(LauncherPrefs prefs)
        {
            prefs.WishlistOfferIds ??= [];
            prefs.BuildSlots ??= [];
            prefs.Achievements ??= new Dictionary<string, bool>();
            prefs.SupportACreator ??= "";
        }
    }
}
