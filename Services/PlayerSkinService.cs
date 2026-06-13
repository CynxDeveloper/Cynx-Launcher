using System.Net.Http;
using System.Text.Json;

namespace SusanooLauncher.Services
{
    internal sealed class PlayerSkinInfo
    {
        public string? SkinName { get; init; }
        public string? SkinIconUrl { get; init; }
        public string? SkinTemplateId { get; init; }
    }

    internal static class PlayerSkinService
    {
        public static async Task<PlayerSkinInfo> FetchAsync()
        {
            try
            {
                using var req = new HttpRequestMessage(HttpMethod.Get, BackendApiClient.Url("/phoenix/api/launcher/profile/skin"));
                BackendApiClient.ApplyAuth(req);
                using HttpResponseMessage resp = await BackendApiClient.Http.SendAsync(req);
                string json = await resp.Content.ReadAsStringAsync();

                if (!resp.IsSuccessStatusCode)
                    return new PlayerSkinInfo();

                using JsonDocument doc = JsonDocument.Parse(json);
                JsonElement root = doc.RootElement;

                return new PlayerSkinInfo
                {
                    SkinName = root.TryGetProperty("skinName", out JsonElement n) ? n.GetString() : null,
                    SkinIconUrl = root.TryGetProperty("skinIconUrl", out JsonElement u) ? u.GetString() : null,
                    SkinTemplateId = root.TryGetProperty("skinTemplateId", out JsonElement t) ? t.GetString() : null,
                };
            }
            catch
            {
                return new PlayerSkinInfo();
            }
        }

        public static PlayerSkinInfo FromAuthJson(string? skinName, string? skinIconUrl, string? skinTemplateId) =>
            new()
            {
                SkinName = skinName,
                SkinIconUrl = skinIconUrl,
                SkinTemplateId = skinTemplateId,
            };
    }
}
