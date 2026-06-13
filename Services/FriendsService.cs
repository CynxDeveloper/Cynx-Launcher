using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SusanooLauncher.Services
{
    internal sealed class FriendEntry
    {
        public string AccountId { get; init; } = "";
        public string DisplayName { get; init; } = "";
        public string Status { get; init; } = "offline";
    }

    internal static class FriendsService
    {
        public static async Task<IReadOnlyList<FriendEntry>> FetchFriendsAsync()
        {
            string? accountId = UserSession.AccountId;
            if (string.IsNullOrWhiteSpace(accountId))
                return [];

            try
            {
                string url = BackendApiClient.Url($"/friends/api/v1/{accountId}/summary");
                using var req = new HttpRequestMessage(HttpMethod.Get, url);
                BackendApiClient.ApplyAuth(req);
                using HttpResponseMessage resp = await BackendApiClient.Http.SendAsync(req);
                if (!resp.IsSuccessStatusCode)
                    return [];

                string json = await resp.Content.ReadAsStringAsync();
                FriendsSummary? summary = JsonSerializer.Deserialize<FriendsSummary>(json);
                if (summary?.Friends == null)
                    return [];

                return summary.Friends.Select(f => new FriendEntry
                {
                    AccountId = f.AccountId ?? "",
                    DisplayName = f.Alias ?? f.AccountId ?? "Unknown",
                    Status = "online",
                }).ToList();
            }
            catch
            {
                return [];
            }
        }

        private sealed class FriendsSummary
        {
            [JsonPropertyName("friends")]
            public List<FriendDto>? Friends { get; set; }
        }

        private sealed class FriendDto
        {
            [JsonPropertyName("accountId")]
            public string? AccountId { get; set; }

            [JsonPropertyName("alias")]
            public string? Alias { get; set; }
        }
    }
}
