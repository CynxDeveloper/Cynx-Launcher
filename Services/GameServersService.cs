using System.Net.Http;
using System.Text.Json;

namespace SusanooLauncher.Services
{
    internal sealed class GameServerSession
    {
        public string SessionId { get; init; } = "";
        public string PlaylistLabel { get; init; } = "";
        public string Status { get; init; } = "";
        public string Region { get; init; } = "";
        public int PlayerCount { get; init; }
        public int MaxPlayers { get; init; }
        public int PlayersLeft { get; init; }
        public string Title { get; init; } = "";
        public string Meta { get; init; } = "";
        public string PlayerCountText { get; init; } = "";
        public string PlayersLeftText { get; init; } = "";
    }

    internal sealed class GameServersSnapshot
    {
        public int ServersOnline { get; init; }
        public int TotalPlayers { get; init; }
        public IReadOnlyList<GameServerSession> Sessions { get; init; } = Array.Empty<GameServerSession>();
    }

    internal static class GameServersService
    {
        public static async Task<GameServersSnapshot> FetchAsync()
        {
            string url = BackendApiClient.Url("/phoenix/api/launcher/servers");
            using HttpResponseMessage resp = await BackendApiClient.Http.GetAsync(url);
            string json = await resp.Content.ReadAsStringAsync();

            if (!resp.IsSuccessStatusCode)
                return Empty();

            using JsonDocument doc = JsonDocument.Parse(json);
            JsonElement root = doc.RootElement;

            int serversOnline = root.TryGetProperty("serversOnline", out JsonElement so) && so.TryGetInt32(out int s)
                ? s
                : 0;
            int totalPlayers = root.TryGetProperty("totalPlayers", out JsonElement tp) && tp.TryGetInt32(out int t)
                ? t
                : 0;

            var sessions = new List<GameServerSession>();
            if (root.TryGetProperty("sessions", out JsonElement arr) && arr.ValueKind == JsonValueKind.Array)
            {
                foreach (JsonElement item in arr.EnumerateArray())
                {
                    sessions.Add(new GameServerSession
                    {
                        SessionId = GetStr(item, "sessionId"),
                        PlaylistLabel = GetStr(item, "playlistLabel"),
                        Status = GetStr(item, "status"),
                        Region = GetStr(item, "region"),
                        PlayerCount = GetInt(item, "playerCount"),
                        MaxPlayers = GetInt(item, "maxPlayers"),
                        PlayersLeft = GetInt(item, "playersLeft"),
                        Title = GetStr(item, "title"),
                        Meta = GetStr(item, "meta"),
                        PlayerCountText = GetStr(item, "playerCountText"),
                        PlayersLeftText = GetStr(item, "playersLeftText"),
                    });
                }
            }

            return new GameServersSnapshot
            {
                ServersOnline = serversOnline,
                TotalPlayers = totalPlayers,
                Sessions = sessions,
            };
        }

        private static GameServersSnapshot Empty() => new();

        private static string GetStr(JsonElement el, string name) =>
            el.TryGetProperty(name, out JsonElement v) ? v.GetString() ?? "" : "";

        private static int GetInt(JsonElement el, string name) =>
            el.TryGetProperty(name, out JsonElement v) && v.TryGetInt32(out int n) ? n : 0;
    }
}
