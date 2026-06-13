using System.Net.Http;
using System.Text.Json;

namespace SusanooLauncher.Services
{
    internal sealed class NewsItem
    {
        public string Title { get; init; } = "";
        public string Body { get; init; } = "";
        public string TabTitle { get; init; } = "News";
    }

    internal static class NewsService
    {
        private const string DefaultLanguage = "en";

        public static async Task<IReadOnlyList<NewsItem>> FetchMotdAsync()
        {
            var items = new List<NewsItem>();

            try
            {
                string url = BackendApiClient.Url("/api/v1/fortnite-br/surfaces/motd/target");
                using var req = new HttpRequestMessage(HttpMethod.Post, url);
                req.Content = new StringContent(
                    JsonSerializer.Serialize(new { language = DefaultLanguage }),
                    System.Text.Encoding.UTF8,
                    "application/json");
                using HttpResponseMessage resp = await BackendApiClient.Http.SendAsync(req);
                string json = await resp.Content.ReadAsStringAsync();

                using JsonDocument doc = JsonDocument.Parse(json);
                if (doc.RootElement.TryGetProperty("contentItems", out JsonElement content) &&
                    content.ValueKind == JsonValueKind.Array)
                {
                    foreach (JsonElement item in content.EnumerateArray())
                    {
                        ParseMotdItem(item, items);
                    }
                }
            }
            catch { }

            if (items.Count == 0)
            {
                items.Add(new NewsItem
                {
                    Title = "Susanoo — Chapter 3 Season 1",
                    Body = "Welcome back! Check the Item Shop for today's cosmetics and compete on the leaderboard.",
                    TabTitle = "Susanoo",
                });
            }

            return items;
        }

        private static void ParseMotdItem(JsonElement item, List<NewsItem> items)
        {
            string title = "";
            string body = "";
            string tab = "News";

            if (item.TryGetProperty("contentFields", out JsonElement fields))
            {
                if (fields.TryGetProperty("title", out JsonElement titleEl))
                    title = ReadLocalizedText(titleEl);
                if (fields.TryGetProperty("body", out JsonElement bodyEl))
                    body = ReadLocalizedText(bodyEl);
                if (fields.TryGetProperty("tabTitleOverride", out JsonElement tabEl))
                    tab = ReadLocalizedText(tabEl);
                if (string.IsNullOrWhiteSpace(tab))
                    tab = "News";
            }

            if (string.IsNullOrWhiteSpace(title) && item.TryGetProperty("title", out JsonElement flatTitle))
                title = ReadLocalizedText(flatTitle);
            if (string.IsNullOrWhiteSpace(body) && item.TryGetProperty("body", out JsonElement flatBody))
                body = ReadLocalizedText(flatBody);
            if (item.TryGetProperty("tabTitleOverride", out JsonElement flatTab))
            {
                string t = ReadLocalizedText(flatTab);
                if (!string.IsNullOrWhiteSpace(t))
                    tab = t;
            }

            if (!string.IsNullOrWhiteSpace(title) || !string.IsNullOrWhiteSpace(body))
                items.Add(new NewsItem { Title = title, Body = body, TabTitle = tab });
        }

        private static string ReadLocalizedText(JsonElement field)
        {
            if (field.ValueKind == JsonValueKind.String)
                return field.GetString() ?? "";

            if (field.ValueKind == JsonValueKind.Object)
            {
                if (field.TryGetProperty(DefaultLanguage, out JsonElement en))
                    return en.GetString() ?? "";
                foreach (JsonProperty prop in field.EnumerateObject())
                {
                    string? value = prop.Value.GetString();
                    if (!string.IsNullOrWhiteSpace(value))
                        return value;
                }
            }

            return "";
        }

        public static async Task<bool> IsBackendOnlineAsync()
        {
            try
            {
                var status = await LauncherStatusService.FetchAsync();
                return status.Online && !status.Maintenance;
            }
            catch
            {
                return false;
            }
        }
    }
}
