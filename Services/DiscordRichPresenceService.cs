using DiscordRPC;
using DiscordRPC.Logging;

namespace SusanooLauncher.Services
{
    internal static class DiscordRichPresenceService
    {
        private static DiscordRpcClient? _client;
        private static bool _started;

        internal static void Start()
        {
            if (_started)
                return;

            var config = LauncherConfig.Load().DiscordRichPresence;
            if (!config.Enabled || string.IsNullOrWhiteSpace(config.ApplicationId))
                return;

            _started = true;

            _client = new DiscordRpcClient(config.ApplicationId.Trim())
            {
                Logger = new ConsoleLogger { Level = LogLevel.Warning }
            };

            _client.OnReady += (_, args) =>
            {
                System.Diagnostics.Debug.WriteLine(
                    $"Discord Rich Presence connected as {args.User.Username}. " +
                    "If Discord still shows 'Phoenix' as the game title, rename the app at " +
                    "https://discord.com/developers/applications (Application ID in launcher.json).");
            };

            if (!_client.Initialize())
            {
                _client.Dispose();
                _client = null;
                return;
            }

            _client.SetPresence(BuildPresence(config));
        }

        internal static void Update(string? details = null, string? state = null)
        {
            if (_client is null || !_client.IsInitialized)
                return;

            var config = LauncherConfig.Load().DiscordRichPresence;
            _client.SetPresence(new RichPresence
            {
                Details = details ?? config.Details,
                State = state ?? config.State,
                Assets = BuildAssets(config),
                Timestamps = Timestamps.Now
            });
        }

        internal static void Shutdown()
        {
            if (_client is null)
                return;

            try
            {
                _client.ClearPresence();
                _client.Dispose();
            }
            catch
            {
                // Discord may already be closed.
            }
            finally
            {
                _client = null;
                _started = false;
            }
        }

        private static RichPresence BuildPresence(DiscordRichPresenceConfig config) =>
            new()
            {
                Details = config.Details,
                State = string.IsNullOrWhiteSpace(config.State) ? null : config.State,
                Assets = BuildAssets(config),
                Timestamps = Timestamps.Now
            };

        private static Assets BuildAssets(DiscordRichPresenceConfig config)
        {
            var assets = new Assets();

            if (!string.IsNullOrWhiteSpace(config.LargeImageKey))
            {
                assets.LargeImageKey = config.LargeImageKey;
                assets.LargeImageText = config.LargeImageText;
            }

            if (!string.IsNullOrWhiteSpace(config.SmallImageKey))
            {
                assets.SmallImageKey = config.SmallImageKey;
                assets.SmallImageText = config.SmallImageText;
            }

            return assets;
        }
    }
}
