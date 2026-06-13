using System.Net.Http;
using System.Text.Json;

namespace SusanooLauncher.Services
{
    internal sealed class UpcomingFeature
    {
        public string Name { get; init; } = "";
        public string Description { get; init; } = "";
        public string Status { get; init; } = "Soon";
    }

    internal sealed class LauncherStatus
    {
        public bool Online { get; init; }
        public int PlayersOnline { get; init; }
        public IReadOnlyList<string> PlayerNames { get; init; } = Array.Empty<string>();
        public string Message { get; init; } = "";
        public string Season { get; init; } = "";
        public bool Maintenance { get; init; }
        public IReadOnlyList<UpcomingFeature> UpcomingFeatures { get; init; } = Array.Empty<UpcomingFeature>();
        public IReadOnlyList<UpcomingFeature> LiveFeatures { get; init; } = Array.Empty<UpcomingFeature>();
        public string GitHubOwner { get; init; } = "";
        public string GitHubRepo { get; init; } = "";
    }

    internal static class LauncherStatusService
    {
        public static async Task<LauncherStatus> FetchAsync()
        {
            try
            {
                string url = BackendApiClient.Url("/phoenix/api/launcher/status");
                using HttpResponseMessage resp = await BackendApiClient.Http.GetAsync(url);
                string json = await resp.Content.ReadAsStringAsync();

                if (!resp.IsSuccessStatusCode)
                    return await FallbackFromXmppAsync();

                using JsonDocument doc = JsonDocument.Parse(json);
                JsonElement root = doc.RootElement;

                int players = root.TryGetProperty("playersOnline", out JsonElement p) && p.TryGetInt32(out int n)
                    ? n
                    : 0;

                var names = new List<string>();
                if (root.TryGetProperty("playerNames", out JsonElement namesEl) &&
                    namesEl.ValueKind == JsonValueKind.Array)
                {
                    foreach (JsonElement item in namesEl.EnumerateArray())
                    {
                        string? name = item.GetString();
                        if (!string.IsNullOrWhiteSpace(name))
                            names.Add(name);
                    }
                }

                var upcoming = ParseFeatureList(root, "upcomingFeatures", "Soon");
                var live = ParseFeatureList(root, "liveFeatures", "Live");

                if (upcoming.Count == 0 && live.Count == 0)
                    upcoming.AddRange(GetLocalUpcomingFallback());

                string ghOwner = "";
                string ghRepo = "";
                if (root.TryGetProperty("launcher", out JsonElement launcherEl))
                {
                    ghOwner = launcherEl.TryGetProperty("githubOwner", out JsonElement o) ? o.GetString() ?? "" : "";
                    ghRepo = launcherEl.TryGetProperty("githubRepo", out JsonElement r) ? r.GetString() ?? "" : "";
                }

                return new LauncherStatus
                {
                    Online = root.TryGetProperty("online", out JsonElement on) && on.ValueKind == JsonValueKind.True,
                    PlayersOnline = players,
                    PlayerNames = names,
                    Message = root.TryGetProperty("message", out JsonElement msg) ? msg.GetString() ?? "" : "",
                    Season = root.TryGetProperty("season", out JsonElement season) ? season.GetString() ?? "" : "",
                    Maintenance = root.TryGetProperty("maintenance", out JsonElement m) && m.ValueKind == JsonValueKind.True,
                    UpcomingFeatures = upcoming,
                    LiveFeatures = live,
                    GitHubOwner = ghOwner,
                    GitHubRepo = ghRepo,
                };
            }
            catch
            {
                return await FallbackFromXmppAsync();
            }
        }

        private static async Task<LauncherStatus> FallbackFromXmppAsync()
        {
            int players = 0;
            var names = new List<string>();
            bool online = false;

            try
            {
                Uri backend = new Uri(UserSession.BackendUrl);
                string xmppRoot = $"{backend.Scheme}://{backend.Host}/";
                using HttpResponseMessage resp = await BackendApiClient.Http.GetAsync(xmppRoot);
                online = resp.IsSuccessStatusCode;
                if (online)
                {
                    string json = await resp.Content.ReadAsStringAsync();
                    using JsonDocument doc = JsonDocument.Parse(json);
                    if (doc.RootElement.TryGetProperty("Clients", out JsonElement clients))
                    {
                        if (clients.TryGetProperty("amount", out JsonElement amount) && amount.TryGetInt32(out int parsed))
                            players = parsed;
                        if (clients.TryGetProperty("clients", out JsonElement list) && list.ValueKind == JsonValueKind.Array)
                        {
                            foreach (JsonElement item in list.EnumerateArray())
                            {
                                string? name = item.GetString();
                                if (!string.IsNullOrWhiteSpace(name))
                                    names.Add(name);
                            }
                        }
                    }
                }
            }
            catch { }

            return new LauncherStatus
            {
                Online = online,
                PlayersOnline = players,
                PlayerNames = names,
                Message = players > 0 ? $"{players} players online" : "Server offline or status unavailable",
                UpcomingFeatures = GetLocalUpcomingFallback(),
            };
        }

        private static List<UpcomingFeature> ParseFeatureList(JsonElement root, string prop, string defaultStatus)
        {
            var list = new List<UpcomingFeature>();
            if (!root.TryGetProperty(prop, out JsonElement featEl) || featEl.ValueKind != JsonValueKind.Array)
                return list;

            foreach (JsonElement item in featEl.EnumerateArray())
            {
                string name = item.TryGetProperty("name", out JsonElement nEl) ? nEl.GetString() ?? "" : "";
                string desc = item.TryGetProperty("description", out JsonElement dEl) ? dEl.GetString() ?? "" : "";
                string status = item.TryGetProperty("status", out JsonElement sEl) ? sEl.GetString() ?? defaultStatus : defaultStatus;
                if (!string.IsNullOrWhiteSpace(name))
                    list.Add(new UpcomingFeature { Name = name, Description = desc, Status = status });
            }

            return list;
        }

        private static List<UpcomingFeature> GetLocalUpcomingFallback() =>
            FeatureRegistry.All
                .Where(f => f.Status == FeatureStatus.Planned || f.Status == FeatureStatus.Beta)
                .Take(6)
                .Select(f => new UpcomingFeature
                {
                    Name = f.Name,
                    Description = f.Description,
                    Status = f.Status == FeatureStatus.Beta ? "Beta" : "Soon",
                })
                .ToList();
    }
}
