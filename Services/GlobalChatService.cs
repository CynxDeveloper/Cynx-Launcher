using System.Net.Http;
using System.Text;
using System.Text.Json;

namespace SusanooLauncher.Services
{
    internal sealed class ChatMessage
    {
        public string Id { get; init; } = "";
        public string DisplayName { get; init; } = "";
        public string Text { get; init; } = "";
        public long SentAt { get; init; }
    }

    internal static class GlobalChatService
    {
        public static async Task<(bool Enabled, IReadOnlyList<ChatMessage> Messages)> FetchMessagesAsync(long sinceMs = 0)
        {
            try
            {
                string url = BackendApiClient.Url($"/phoenix/api/launcher/chat/messages?since={sinceMs}");
                using var req = new HttpRequestMessage(HttpMethod.Get, url);
                BackendApiClient.ApplyAuth(req);
                using HttpResponseMessage resp = await BackendApiClient.Http.SendAsync(req);
                string json = await resp.Content.ReadAsStringAsync();
                using JsonDocument doc = JsonDocument.Parse(json);

                bool enabled = !doc.RootElement.TryGetProperty("enabled", out JsonElement en) ||
                               en.ValueKind == JsonValueKind.True;

                var list = new List<ChatMessage>();
                if (doc.RootElement.TryGetProperty("messages", out JsonElement messages) &&
                    messages.ValueKind == JsonValueKind.Array)
                {
                    foreach (JsonElement item in messages.EnumerateArray())
                    {
                        list.Add(new ChatMessage
                        {
                            Id = item.TryGetProperty("id", out JsonElement id) ? id.GetString() ?? "" : "",
                            DisplayName = item.TryGetProperty("displayName", out JsonElement n) ? n.GetString() ?? "" : "",
                            Text = item.TryGetProperty("text", out JsonElement t) ? t.GetString() ?? "" : "",
                            SentAt = item.TryGetProperty("sentAt", out JsonElement s) && s.TryGetInt64(out long ms) ? ms : 0,
                        });
                    }
                }

                return (enabled, list);
            }
            catch
            {
                return (false, Array.Empty<ChatMessage>());
            }
        }

        public static async Task<(bool Success, string Message)> SendAsync(string text)
        {
            try
            {
                using var req = new HttpRequestMessage(HttpMethod.Post, BackendApiClient.Url("/phoenix/api/launcher/chat/send"));
                BackendApiClient.ApplyAuth(req);
                req.Content = new StringContent(
                    JsonSerializer.Serialize(new { message = text }),
                    Encoding.UTF8,
                    "application/json");

                using HttpResponseMessage resp = await BackendApiClient.Http.SendAsync(req);
                string json = await resp.Content.ReadAsStringAsync();
                using JsonDocument doc = JsonDocument.Parse(json);
                bool ok = doc.RootElement.TryGetProperty("success", out JsonElement s) &&
                          s.ValueKind == JsonValueKind.True;
                string msg = doc.RootElement.TryGetProperty("message", out JsonElement m)
                    ? m.GetString() ?? (ok ? "Sent." : "Failed.")
                    : (ok ? "Sent." : "Failed.");
                return (ok, msg);
            }
            catch (Exception ex)
            {
                return (false, ex.Message);
            }
        }
    }
}
