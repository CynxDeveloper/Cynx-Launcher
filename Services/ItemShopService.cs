using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using SusanooLauncher.Models;

namespace SusanooLauncher.Services
{
    internal sealed class ItemShopService
    {
        private const string FortniteUserAgent =
            GameVersionConstants.UserAgent;

        private static readonly HttpClient _http = CreateHttpClient();

        private static readonly Dictionary<string, CosmeticDetails> _cosmeticCache = new(StringComparer.OrdinalIgnoreCase);

        internal string? LastError { get; private set; }

        private static HttpClient CreateHttpClient()
        {
            var client = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
            client.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", FortniteUserAgent);
            return client;
        }

        internal async Task<IReadOnlyList<ShopItem>> FetchAsync(
            string? backendUrl = null,
            CancellationToken cancellationToken = default)
        {
            LastError = null;
            string baseUrl = string.IsNullOrWhiteSpace(backendUrl)
                ? UserSession.BackendUrl
                : backendUrl.Trim();

            string catalogUrl = $"{baseUrl.TrimEnd('/')}/fortnite/api/storefront/v2/catalog";

            try
            {
                using HttpResponseMessage response = await _http.GetAsync(catalogUrl, cancellationToken);
                string body = await response.Content.ReadAsStringAsync(cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    LastError = $"Could not load item shop (HTTP {(int)response.StatusCode}).";
                    return [];
                }

                using JsonDocument doc = JsonDocument.Parse(body);
                List<RawOffer> offers = ParseCatalog(doc.RootElement);
                if (offers.Count == 0)
                {
                    LastError = "Item shop is empty.";
                    return [];
                }

                await EnrichCosmeticsAsync(offers, cancellationToken);

                return offers.Select(o => new ShopItem
                {
                    Name = o.Name,
                    Price = o.Price,
                    ImageUrl = o.ImageUrl,
                    Section = o.Section,
                    TemplateId = o.TemplateId,
                    CosmeticId = o.CosmeticId,
                    OfferId = o.OfferId,
                    Rarity = o.Rarity,
                }).ToList();
            }
            catch (Exception ex)
            {
                LastError = $"Failed to load item shop.\n\n{ex.Message}";
                return [];
            }
        }

        private static List<RawOffer> ParseCatalog(JsonElement root)
        {
            var offers = new List<RawOffer>();

            if (!root.TryGetProperty("storefronts", out JsonElement storefronts) ||
                storefronts.ValueKind != JsonValueKind.Array)
                return offers;

            foreach (JsonElement storefront in storefronts.EnumerateArray())
            {
                if (!storefront.TryGetProperty("name", out JsonElement nameEl))
                    continue;

                string storefrontName = nameEl.GetString() ?? "";
                string? section = storefrontName switch
                {
                    "BRDailyStorefront" => "Daily",
                    "BRWeeklyStorefront" => "Featured",
                    _ => null,
                };

                if (section == null)
                    continue;

                if (!storefront.TryGetProperty("catalogEntries", out JsonElement entries) ||
                    entries.ValueKind != JsonValueKind.Array)
                    continue;

                foreach (JsonElement entry in entries.EnumerateArray())
                {
                    string? templateId = GetPrimaryGrantTemplateId(entry);
                    if (string.IsNullOrWhiteSpace(templateId))
                        continue;

                    if (!IsBattleRoyaleCosmetic(templateId))
                        continue;

                    int price = GetFinalPrice(entry);
                    if (price <= 0)
                        continue;

                    string cosmeticId = ExtractCosmeticId(templateId);
                    offers.Add(new RawOffer
                    {
                        TemplateId = templateId,
                        CosmeticId = cosmeticId,
                        OfferId = GetOfferId(entry),
                        Price = price,
                        Section = section,
                        Name = FormatCosmeticName(cosmeticId),
                        ImageUrl = BuildIconUrl(cosmeticId),
                    });
                }
            }

            return offers;
        }

        private async Task EnrichCosmeticsAsync(List<RawOffer> offers, CancellationToken cancellationToken)
        {
            var ids = offers
                .Select(o => o.CosmeticId)
                .Where(id => !string.IsNullOrWhiteSpace(id))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Where(id => !_cosmeticCache.ContainsKey(id))
                .ToList();

            if (ids.Count == 0)
                return;

            const int batchSize = 40;
            for (int i = 0; i < ids.Count; i += batchSize)
            {
                var batch = ids.Skip(i).Take(batchSize).ToList();
                var resolved = await FetchCosmeticBatchAsync(batch, cancellationToken);
                foreach (var pair in resolved)
                    _cosmeticCache[pair.Key] = pair.Value;
            }

            foreach (RawOffer offer in offers)
            {
                if (!_cosmeticCache.TryGetValue(offer.CosmeticId, out CosmeticDetails? details))
                    continue;

                if (!string.IsNullOrWhiteSpace(details.Name))
                    offer.Name = details.Name;
                if (!string.IsNullOrWhiteSpace(details.IconUrl))
                    offer.ImageUrl = details.IconUrl;
                if (!string.IsNullOrWhiteSpace(details.Rarity))
                    offer.Rarity = details.Rarity;
            }
        }

        private static string GetOfferId(JsonElement entry)
        {
            if (entry.TryGetProperty("offerId", out JsonElement offer) && offer.ValueKind == JsonValueKind.String)
                return offer.GetString() ?? "";
            if (entry.TryGetProperty("devName", out JsonElement dev) && dev.ValueKind == JsonValueKind.String)
                return dev.GetString() ?? "";
            return "";
        }

        private static async Task<Dictionary<string, CosmeticDetails>> FetchCosmeticBatchAsync(
            IReadOnlyList<string> cosmeticIds,
            CancellationToken cancellationToken)
        {
            var result = new Dictionary<string, CosmeticDetails>(StringComparer.OrdinalIgnoreCase);
            if (cosmeticIds.Count == 0)
                return result;

            string query = string.Join("&", cosmeticIds.Select(id => $"id={Uri.EscapeDataString(id)}"));
            string url = $"https://fortnite-api.com/v2/cosmetics/br/search/ids?language=en&{query}";

            try
            {
                using HttpResponseMessage response = await _http.GetAsync(url, cancellationToken);
                if (!response.IsSuccessStatusCode)
                    return result;

                string json = await response.Content.ReadAsStringAsync(cancellationToken);
                FortniteApiCosmeticResponse? parsed =
                    JsonSerializer.Deserialize<FortniteApiCosmeticResponse>(json);

                if (parsed?.Data == null)
                    return result;

                foreach (FortniteApiCosmetic item in parsed.Data)
                {
                    if (string.IsNullOrWhiteSpace(item.Id))
                        continue;

                    result[item.Id] = new CosmeticDetails
                    {
                        Name = item.Name ?? FormatCosmeticName(item.Id),
                        IconUrl = item.Images?.Icon
                            ?? item.Images?.SmallIcon
                            ?? BuildIconUrl(item.Id),
                        Rarity = item.Rarity?.Value ?? "common",
                    };
                }
            }
            catch
            {
                // Use formatted names / constructed URLs when API is unavailable.
            }

            return result;
        }

        private static string? GetPrimaryGrantTemplateId(JsonElement entry)
        {
            if (!entry.TryGetProperty("itemGrants", out JsonElement grants) ||
                grants.ValueKind != JsonValueKind.Array)
                return null;

            foreach (JsonElement grant in grants.EnumerateArray())
            {
                if (grant.TryGetProperty("templateId", out JsonElement tid))
                {
                    string? value = tid.GetString();
                    if (!string.IsNullOrWhiteSpace(value))
                        return value;
                }
            }

            return null;
        }

        private static int GetFinalPrice(JsonElement entry)
        {
            if (!entry.TryGetProperty("prices", out JsonElement prices) ||
                prices.ValueKind != JsonValueKind.Array)
                return 0;

            foreach (JsonElement price in prices.EnumerateArray())
            {
                if (price.TryGetProperty("finalPrice", out JsonElement final) &&
                    final.TryGetInt32(out int value))
                    return value;

                if (price.TryGetProperty("regularPrice", out JsonElement regular) &&
                    regular.TryGetInt32(out int reg))
                    return reg;
            }

            return 0;
        }

        private static string ExtractCosmeticId(string templateId)
        {
            int colon = templateId.IndexOf(':');
            return colon >= 0 ? templateId[(colon + 1)..] : templateId;
        }

        private static bool IsBattleRoyaleCosmetic(string templateId)
        {
            int colon = templateId.IndexOf(':');
            if (colon < 0)
                return false;

            string type = templateId[..colon];
            return type.StartsWith("Athena", StringComparison.OrdinalIgnoreCase);
        }

        private static string FormatCosmeticName(string cosmeticId)
        {
            string slug = cosmeticId.Replace('_', '-').ToLowerInvariant();
            return slug.Replace('-', ' ').ToUpperInvariant();
        }

        private static string BuildIconUrl(string cosmeticId)
        {
            string slug = cosmeticId.Replace('_', '-').ToLowerInvariant();
            return $"https://fortnite-api.com/images/cosmetics/br/{slug}/icon.png";
        }

        private sealed class RawOffer
        {
            public string TemplateId { get; set; } = "";
            public string CosmeticId { get; set; } = "";
            public string OfferId { get; set; } = "";
            public string Name { get; set; } = "";
            public string ImageUrl { get; set; } = "";
            public string Rarity { get; set; } = "common";
            public int Price { get; set; }
            public string Section { get; set; } = "Daily";
        }

        private sealed class CosmeticDetails
        {
            public string Name { get; init; } = "";
            public string IconUrl { get; init; } = "";
            public string Rarity { get; init; } = "common";
        }

        private sealed class FortniteApiCosmeticResponse
        {
            [JsonPropertyName("data")]
            public List<FortniteApiCosmetic>? Data { get; set; }
        }

        private sealed class FortniteApiCosmetic
        {
            [JsonPropertyName("id")]
            public string? Id { get; set; }

            [JsonPropertyName("name")]
            public string? Name { get; set; }

            [JsonPropertyName("images")]
            public FortniteApiImages? Images { get; set; }

            [JsonPropertyName("rarity")]
            public FortniteApiRarity? Rarity { get; set; }
        }

        private sealed class FortniteApiRarity
        {
            [JsonPropertyName("value")]
            public string? Value { get; set; }
        }

        private sealed class FortniteApiImages
        {
            [JsonPropertyName("icon")]
            public string? Icon { get; set; }

            [JsonPropertyName("smallIcon")]
            public string? SmallIcon { get; set; }
        }
    }
}
