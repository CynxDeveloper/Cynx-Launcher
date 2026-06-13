using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SusanooLauncher.Services
{
    internal sealed class LauncherConfigFile
    {
        [JsonPropertyName("MongoConnectionString")]
        public string MongoConnectionString { get; set; } = "mongodb://26.157.83.30:27017";

        [JsonPropertyName("MongoDatabase")]
        public string MongoDatabase { get; set; } = "Susanoo";

        [JsonPropertyName("MongoCollection")]
        public string MongoCollection { get; set; } = "tournament_standings";

        [JsonPropertyName("ArenaEventId")]
        public string ArenaEventId { get; set; } = "epicgames_Arena_S24_Solo";

        [JsonPropertyName("LeaderboardApiUrl")]
        public string LeaderboardApiUrl { get; set; } = "";

        [JsonPropertyName("UseMongoDirect")]
        public bool UseMongoDirect { get; set; } = false;

        [JsonPropertyName("LeaderboardRefreshSeconds")]
        public int LeaderboardRefreshSeconds { get; set; } = 3600;

        [JsonPropertyName("DiscordRichPresence")]
        public DiscordRichPresenceConfig DiscordRichPresence { get; set; } = new();
    }

    internal sealed class DiscordRichPresenceConfig
    {
        [JsonPropertyName("Enabled")]
        public bool Enabled { get; set; } = true;

        /// <summary>
        /// Discord application ID from https://discord.com/developers/applications
        /// </summary>
        [JsonPropertyName("ApplicationId")]
        public string ApplicationId { get; set; } = "";

        [JsonPropertyName("Details")]
        public string Details { get; set; } = "Susanoo";

        [JsonPropertyName("State")]
        public string State { get; set; } = "An OG Fortnite Experience.";

        /// <summary>Art asset key from Discord Developer Portal → Rich Presence → Art Assets.</summary>
        [JsonPropertyName("LargeImageKey")]
        public string LargeImageKey { get; set; } = "susanoo";

        [JsonPropertyName("LargeImageText")]
        public string LargeImageText { get; set; } = "Susanoo Launcher";

        [JsonPropertyName("SmallImageKey")]
        public string SmallImageKey { get; set; } = "character";

        [JsonPropertyName("SmallImageText")]
        public string SmallImageText { get; set; } = "";
    }

    internal static class LauncherConfig
    {
        private static LauncherConfigFile? _cached;

        internal static LauncherConfigFile Load()
        {
            if (_cached != null)
                return _cached;

            string path = Path.Join(AppContext.BaseDirectory, "launcher.json");
            if (File.Exists(path))
            {
                try
                {
                    _cached = JsonSerializer.Deserialize<LauncherConfigFile>(File.ReadAllText(path)) ?? new LauncherConfigFile();
                    return _cached;
                }
                catch
                {
                    // fall through
                }
            }

            _cached = new LauncherConfigFile();
            return _cached;
        }

        internal static void Invalidate() => _cached = null;
    }
}
