using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SusanooLauncher.Services
{
    internal sealed class LauncherAuthResult
    {
        public bool Success { get; init; }
        public bool Banned { get; init; }
        public bool IpBanned { get; init; }
        public bool NotRegistered { get; init; }
        public bool AlreadyRegistered { get; init; }
        public bool InvalidCredentials { get; init; }
        public bool HwidLocked { get; init; }
        public string Message { get; init; } = "";
        public string? Username { get; init; }
        public string? AccountId { get; init; }
        public string? AccessToken { get; init; }
        public string? SkinName { get; init; }
        public string? SkinIconUrl { get; init; }
        public string? SkinTemplateId { get; init; }
    }

    internal static class LauncherAuthService
    {
        private static readonly HttpClient Http = new();
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true,
        };

        internal static async Task<LauncherAuthResult> LoginAsync(
            string backendUrl,
            string email,
            string password,
            string hwid)
        {
            return await PostAsync(backendUrl, "/phoenix/api/launcher/login", new { email, password, hwid });
        }

        internal static async Task<LauncherAuthResult> RegisterAsync(
            string backendUrl,
            string email,
            string password,
            string username,
            string discordId,
            string hwid)
        {
            return await PostAsync(
                backendUrl,
                "/phoenix/api/launcher/register",
                new { email, password, username, discordId, hwid });
        }

        private static async Task<LauncherAuthResult> PostAsync(string backendUrl, string path, object body)
        {
            try
            {
                string url = backendUrl.TrimEnd('/') + path;
                string json = JsonSerializer.Serialize(body);
                using var content = new StringContent(json, Encoding.UTF8, "application/json");
                using HttpResponseMessage resp = await Http.PostAsync(url, content);
                string responseBody = await resp.Content.ReadAsStringAsync();

                LauncherAuthResponse? parsed;
                try
                {
                    parsed = JsonSerializer.Deserialize<LauncherAuthResponse>(responseBody, JsonOptions);
                }
                catch
                {
                    return new LauncherAuthResult
                    {
                        Success = false,
                        Message = "Could not read the server response.",
                    };
                }

                if (parsed == null)
                {
                    return new LauncherAuthResult
                    {
                        Success = false,
                        Message = "Empty response from server.",
                    };
                }

                if (parsed.Success)
                {
                    return new LauncherAuthResult
                    {
                        Success = true,
                        Username = parsed.Username,
                        AccountId = parsed.AccountId,
                        AccessToken = parsed.AccessToken,
                        Message = parsed.Message ?? "",
                        SkinName = parsed.SkinName,
                        SkinIconUrl = parsed.SkinIconUrl,
                        SkinTemplateId = parsed.SkinTemplateId,
                    };
                }

                return new LauncherAuthResult
                {
                    Success = false,
                    Banned = parsed.Banned,
                    IpBanned = parsed.IpBanned,
                    NotRegistered = parsed.NotRegistered,
                    AlreadyRegistered = parsed.AlreadyRegistered,
                    InvalidCredentials = parsed.InvalidCredentials,
                    HwidLocked = parsed.HwidLocked,
                    Message = parsed.Message ?? "Login failed.",
                };
            }
            catch (Exception ex)
            {
                return new LauncherAuthResult
                {
                    Success = false,
                    Message = $"Could not reach the backend.\n\n{ex.Message}",
                };
            }
        }

        private sealed class LauncherAuthResponse
        {
            [JsonPropertyName("success")]
            public bool Success { get; set; }

            [JsonPropertyName("banned")]
            public bool Banned { get; set; }

            [JsonPropertyName("ipBanned")]
            public bool IpBanned { get; set; }

            [JsonPropertyName("notRegistered")]
            public bool NotRegistered { get; set; }

            [JsonPropertyName("alreadyRegistered")]
            public bool AlreadyRegistered { get; set; }

            [JsonPropertyName("invalidCredentials")]
            public bool InvalidCredentials { get; set; }

            [JsonPropertyName("hwidLocked")]
            public bool HwidLocked { get; set; }

            [JsonPropertyName("message")]
            public string? Message { get; set; }

            [JsonPropertyName("username")]
            public string? Username { get; set; }

            [JsonPropertyName("accountId")]
            public string? AccountId { get; set; }

            [JsonPropertyName("accessToken")]
            public string? AccessToken { get; set; }

            [JsonPropertyName("skinName")]
            public string? SkinName { get; set; }

            [JsonPropertyName("skinIconUrl")]
            public string? SkinIconUrl { get; set; }

            [JsonPropertyName("skinTemplateId")]
            public string? SkinTemplateId { get; set; }
        }
    }
}
