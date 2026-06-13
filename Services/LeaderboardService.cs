using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using SusanooLauncher.Models;
using MongoDB.Bson;
using MongoDB.Driver;

namespace SusanooLauncher.Services
{
    internal sealed class LeaderboardService
    {
        private static readonly HttpClient _http = new HttpClient { Timeout = TimeSpan.FromSeconds(25) };

        internal string? LastError { get; private set; }

        private sealed class ApiLeaderboardResponse
        {
            [JsonPropertyName("entries")]
            public List<ApiEntry>? Entries { get; set; }

            [JsonPropertyName("nextUpdateSeconds")]
            public int? NextUpdateSeconds { get; set; }
        }

        private sealed class ApiEntry
        {
            [JsonPropertyName("displayName")]
            public string? DisplayName { get; set; }

            [JsonPropertyName("accountId")]
            public string? AccountId { get; set; }

            [JsonPropertyName("account")]
            public string? Account { get; set; }

            [JsonPropertyName("hypeArenaPoints")]
            public long HypeArenaPoints { get; set; }

            [JsonPropertyName("points")]
            public long Points { get; set; }

            [JsonPropertyName("value")]
            public long Value { get; set; }

            [JsonPropertyName("kills")]
            public long Kills { get; set; }

            [JsonPropertyName("wins")]
            public long Wins { get; set; }

            [JsonPropertyName("level")]
            public int Level { get; set; }

            [JsonPropertyName("skinTemplateId")]
            public string? SkinTemplateId { get; set; }

            [JsonPropertyName("skinName")]
            public string? SkinName { get; set; }

            [JsonPropertyName("skinIconUrl")]
            public string? SkinIconUrl { get; set; }
        }

        private sealed class StatsV2Response
        {
            [JsonPropertyName("entries")]
            public List<ApiEntry>? Entries { get; set; }
        }

        internal async Task<(IReadOnlyList<LeaderboardEntry> Entries, int NextUpdateSeconds)> FetchAsync(
            string sort,
            string? backendUrl = null,
            CancellationToken cancellationToken = default)
        {
            LastError = null;
            var cfg = LauncherConfig.Load();
            string backend = NormalizeBackend(backendUrl ?? Settings.Default.backend);
            int refresh = cfg.LeaderboardRefreshSeconds;

            if (!string.IsNullOrWhiteSpace(backend))
            {
                try
                {
                    var kairo = await FetchFromKairoLauncherApiAsync(backend, sort, cancellationToken);
                    if (kairo.Entries.Count > 0)
                        return kairo;

                    var stats = await FetchFromKairoStatsApiAsync(backend, sort, cancellationToken);
                    if (stats.Entries.Count > 0)
                        return stats;
                }
                catch (Exception ex)
                {
                    LastError = ex.Message;
                }
            }

            if (!string.IsNullOrWhiteSpace(cfg.LeaderboardApiUrl))
            {
                try
                {
                    var legacy = await FetchFromLegacyApiAsync(cfg, sort, cancellationToken);
                    if (legacy.Entries.Count > 0)
                        return legacy;
                }
                catch (Exception ex)
                {
                    LastError = $"API: {ex.Message}";
                }
            }

            if (cfg.UseMongoDirect && !string.IsNullOrWhiteSpace(cfg.MongoConnectionString))
            {
                try
                {
                    var mongo = await FetchFromMongoAsync(cfg, sort, cancellationToken);
                    if (mongo.Count > 0)
                        return (mongo, refresh);
                }
                catch (Exception ex)
                {
                    LastError = $"MongoDB: {ex.Message}";
                }
            }

            if (LastError == null)
                LastError = "No leaderboard data. Start the Kairo backend and play Arena matches.";

            return (Array.Empty<LeaderboardEntry>(), refresh);
        }

        private static string NormalizeBackend(string? url)
        {
            if (string.IsNullOrWhiteSpace(url))
                return "";
            return url.Trim().TrimEnd('/');
        }

        private static async Task<(IReadOnlyList<LeaderboardEntry> Entries, int NextUpdateSeconds)> FetchFromKairoLauncherApiAsync(
            string backend,
            string sort,
            CancellationToken cancellationToken)
        {
            string url = $"{backend}/phoenix/api/launcher/leaderboard?sort={Uri.EscapeDataString(sort)}&limit=100";
            using HttpResponseMessage resp = await _http.GetAsync(url, cancellationToken);
            string body = await resp.Content.ReadAsStringAsync(cancellationToken);

            if (!resp.IsSuccessStatusCode)
                throw new InvalidOperationException($"Kairo backend HTTP {(int)resp.StatusCode}: {body}");

            ApiLeaderboardResponse? data = JsonSerializer.Deserialize<ApiLeaderboardResponse>(body);
            if (data?.Entries == null || data.Entries.Count == 0)
                return (Array.Empty<LeaderboardEntry>(), LauncherConfig.Load().LeaderboardRefreshSeconds);

            return (MapAndRank(data.Entries, sort), data.NextUpdateSeconds ?? LauncherConfig.Load().LeaderboardRefreshSeconds);
        }

        private static async Task<(IReadOnlyList<LeaderboardEntry> Entries, int NextUpdateSeconds)> FetchFromKairoStatsApiAsync(
            string backend,
            string sort,
            CancellationToken cancellationToken)
        {
            string leaderboardName = sort switch
            {
                "kills" => "br_kills_keyboardmouse_m0_playlist_defaultsolo",
                "wins" => "br_placetop1_keyboardmouse_m0_playlist_defaultsolo",
                _ => "hype_Phoenix_arena",
            };

            string url = $"{backend}/fortnite/api/statsv2/leaderboards/{leaderboardName}?maxSize=100";
            using HttpResponseMessage resp = await _http.GetAsync(url, cancellationToken);
            string body = await resp.Content.ReadAsStringAsync(cancellationToken);

            if (!resp.IsSuccessStatusCode)
                throw new InvalidOperationException($"Stats API HTTP {(int)resp.StatusCode}: {body}");

            StatsV2Response? data = JsonSerializer.Deserialize<StatsV2Response>(body);
            if (data?.Entries == null)
                return (Array.Empty<LeaderboardEntry>(), LauncherConfig.Load().LeaderboardRefreshSeconds);

            return (MapAndRank(data.Entries, sort), LauncherConfig.Load().LeaderboardRefreshSeconds);
        }

        private static async Task<(IReadOnlyList<LeaderboardEntry> Entries, int NextUpdateSeconds)> FetchFromLegacyApiAsync(
            LauncherConfigFile cfg,
            string sort,
            CancellationToken cancellationToken)
        {
            string url = $"{cfg.LeaderboardApiUrl.TrimEnd('/')}?sort={Uri.EscapeDataString(sort)}&limit=100";
            using HttpResponseMessage resp = await _http.GetAsync(url, cancellationToken);
            string body = await resp.Content.ReadAsStringAsync(cancellationToken);

            if (!resp.IsSuccessStatusCode)
                throw new InvalidOperationException($"HTTP {(int)resp.StatusCode}: {body}");

            ApiLeaderboardResponse? data = JsonSerializer.Deserialize<ApiLeaderboardResponse>(body);
            if (data?.Entries == null)
            {
                var list = JsonSerializer.Deserialize<List<ApiEntry>>(body);
                if (list == null)
                    return (Array.Empty<LeaderboardEntry>(), cfg.LeaderboardRefreshSeconds);
                data = new ApiLeaderboardResponse { Entries = list };
            }

            return (MapAndRank(data.Entries, sort), data.NextUpdateSeconds ?? cfg.LeaderboardRefreshSeconds);
        }

        private static async Task<IReadOnlyList<LeaderboardEntry>> FetchFromMongoAsync(
            LauncherConfigFile cfg,
            string sort,
            CancellationToken cancellationToken)
        {
            var client = new MongoClient(cfg.MongoConnectionString);
            var database = client.GetDatabase(cfg.MongoDatabase);
            var standings = database.GetCollection<BsonDocument>("tournament_standings");
            var userStats = database.GetCollection<BsonDocument>("userstats");
            var users = database.GetCollection<BsonDocument>("users");

            string eventId = cfg.ArenaEventId;
            var arenaRows = await standings.Find(Builders<BsonDocument>.Filter.Eq("eventId", eventId))
                .Sort(Builders<BsonDocument>.Sort.Descending("points"))
                .Limit(150)
                .ToListAsync(cancellationToken);

            var apiEntries = new List<ApiEntry>();
            foreach (BsonDocument row in arenaRows)
            {
                string accountId = ReadString(row, "accountId");
                if (string.IsNullOrWhiteSpace(accountId))
                    continue;

                var user = await users.Find(Builders<BsonDocument>.Filter.Eq("accountId", accountId))
                    .FirstOrDefaultAsync(cancellationToken);
                if (user == null)
                    continue;

                var stat = await userStats.Find(Builders<BsonDocument>.Filter.Eq("accountId", accountId))
                    .FirstOrDefaultAsync(cancellationToken);

                BsonDocument? solo = null;
                if (stat != null && stat.Contains("solo") && stat["solo"].IsBsonDocument)
                    solo = stat["solo"].AsBsonDocument;

                apiEntries.Add(new ApiEntry
                {
                    DisplayName = ReadString(user, "username", "displayName"),
                    AccountId = accountId,
                    HypeArenaPoints = ReadLong(row, "points"),
                    Kills = solo != null ? ReadLong(solo, "kills") : 0,
                    Wins = solo != null ? ReadLong(solo, "placetop1") : 0,
                    Level = row.Contains("division") ? row["division"].ToInt32() + 1 : 1,
                });
            }

            if (apiEntries.Count == 0)
            {
                var docs = await database.GetCollection<BsonDocument>(cfg.MongoCollection)
                    .Find(FilterDefinition<BsonDocument>.Empty)
                    .Sort(Builders<BsonDocument>.Sort.Descending(
                        sort == "kills" ? "kills" : sort == "wins" ? "wins" : "hypeArenaPoints"))
                    .Limit(100)
                    .ToListAsync(cancellationToken);

                foreach (BsonDocument doc in docs)
                {
                    apiEntries.Add(new ApiEntry
                    {
                        DisplayName = ReadString(doc, "displayName", "username", "name"),
                        AccountId = ReadString(doc, "accountId", "id"),
                        HypeArenaPoints = ReadLong(doc, "hypeArenaPoints", "points", "hype"),
                        Kills = ReadLong(doc, "kills"),
                        Wins = ReadLong(doc, "wins"),
                        Level = doc.Contains("level") ? doc["level"].ToInt32() : 1,
                    });
                }
            }

            return MapAndRank(apiEntries, sort);
        }

        private static string ReadString(BsonDocument doc, params string[] keys)
        {
            foreach (string key in keys)
            {
                if (doc.Contains(key) && !doc[key].IsBsonNull)
                    return doc[key].ToString() ?? "";
            }
            return "";
        }

        private static long ReadLong(BsonDocument doc, params string[] keys)
        {
            foreach (string key in keys)
            {
                if (!doc.Contains(key))
                    continue;

                BsonValue v = doc[key];
                if (v.IsInt64) return v.AsInt64;
                if (v.IsInt32) return v.AsInt32;
                if (v.IsDouble) return (long)v.AsDouble;
            }
            return 0;
        }

        private static IReadOnlyList<LeaderboardEntry> MapAndRank(List<ApiEntry> source, string sort)
        {
            var sorted = source
                .Select(e => new LeaderboardEntry
                {
                    DisplayName = string.IsNullOrWhiteSpace(e.DisplayName) ? "Unknown" : e.DisplayName!,
                    AccountId = !string.IsNullOrWhiteSpace(e.AccountId) ? e.AccountId! : (e.Account ?? ""),
                    HypeArenaPoints = e.HypeArenaPoints > 0 ? e.HypeArenaPoints : (e.Points > 0 ? e.Points : e.Value),
                    Kills = e.Kills,
                    Wins = e.Wins,
                    Level = e.Level > 0 ? e.Level : 1,
                    SkinTemplateId = e.SkinTemplateId ?? "",
                    SkinName = e.SkinName ?? "",
                    SkinIconUrl = e.SkinIconUrl ?? "",
                })
                .OrderByDescending(e => e.GetSortValue(sort))
                .ThenBy(e => e.DisplayName, StringComparer.OrdinalIgnoreCase)
                .Take(100)
                .ToList();

            for (int i = 0; i < sorted.Count; i++)
                sorted[i].Rank = i + 1;

            return sorted;
        }
    }
}
