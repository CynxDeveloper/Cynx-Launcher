using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SusanooLauncher.Services
{
    internal static class BanCheckService
    {
        public static async Task<(bool Banned, bool IpBanned, string Message)> CheckEmailAsync(string email)
        {
            try
            {
                string url = BackendApiClient.Url($"/phoenix/api/launcher/check?email={Uri.EscapeDataString(email)}");
                using HttpResponseMessage resp = await BackendApiClient.Http.GetAsync(url);
                string body = await resp.Content.ReadAsStringAsync();

                CheckResponse? parsed = JsonSerializer.Deserialize<CheckResponse>(body);
                if (parsed?.Banned != true)
                    return (false, false, "");

                if (parsed.IpBanned)
                {
                    return (true, true,
                        string.IsNullOrWhiteSpace(parsed.Message)
                            ? "You Are Ip Banned From Susanoo! If this was by accident feel free to open a ticket on support server!!"
                            : parsed.Message);
                }

                return (true, false,
                    string.IsNullOrWhiteSpace(parsed.Message)
                        ? "This account is banned and cannot play."
                        : parsed.Message);
            }
            catch
            {
                return (false, false, "");
            }
        }

        private sealed class CheckResponse
        {
            [JsonPropertyName("banned")]
            public bool Banned { get; set; }

            [JsonPropertyName("ipBanned")]
            public bool IpBanned { get; set; }

            [JsonPropertyName("message")]
            public string? Message { get; set; }
        }
    }
}
