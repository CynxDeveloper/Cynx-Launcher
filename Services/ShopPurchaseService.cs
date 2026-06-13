using System.Net.Http;
using System.Text;
using System.Text.Json;

namespace SusanooLauncher.Services
{
    internal sealed class PurchaseResult
    {
        public bool Success { get; init; }
        public string Message { get; init; } = "";
        public int Balance { get; init; }
        public int Price { get; init; }
    }

    internal static class ShopPurchaseService
    {
        public static async Task<int> GetBalanceAsync()
        {
            try
            {
                using var req = new HttpRequestMessage(HttpMethod.Get, BackendApiClient.Url("/phoenix/api/launcher/shop/balance"));
                BackendApiClient.ApplyAuth(req);
                using HttpResponseMessage resp = await BackendApiClient.Http.SendAsync(req);
                string json = await resp.Content.ReadAsStringAsync();
                using JsonDocument doc = JsonDocument.Parse(json);
                if (doc.RootElement.TryGetProperty("balance", out JsonElement b) && b.TryGetInt32(out int balance))
                    return balance;
            }
            catch { }

            return -1;
        }

        public static async Task<PurchaseResult> PurchaseAsync(string offerId)
        {
            if (string.IsNullOrWhiteSpace(offerId))
                return new PurchaseResult { Success = false, Message = "Invalid offer." };

            try
            {
                using var req = new HttpRequestMessage(HttpMethod.Post, BackendApiClient.Url("/phoenix/api/launcher/shop/purchase"));
                BackendApiClient.ApplyAuth(req);
                req.Content = new StringContent(
                    JsonSerializer.Serialize(new { offerId }),
                    Encoding.UTF8,
                    "application/json");

                using HttpResponseMessage resp = await BackendApiClient.Http.SendAsync(req);
                string json = await resp.Content.ReadAsStringAsync();
                using JsonDocument doc = JsonDocument.Parse(json);
                JsonElement root = doc.RootElement;

                bool success = root.TryGetProperty("success", out JsonElement s) &&
                                 s.ValueKind == JsonValueKind.True;
                string message = root.TryGetProperty("message", out JsonElement m) ? m.GetString() ?? "" : "";
                int balance = root.TryGetProperty("balance", out JsonElement b) && b.TryGetInt32(out int bal) ? bal : -1;
                int price = root.TryGetProperty("price", out JsonElement p) && p.TryGetInt32(out int pr) ? pr : 0;

                return new PurchaseResult
                {
                    Success = success,
                    Message = string.IsNullOrWhiteSpace(message)
                        ? (success ? "Purchase complete!" : "Purchase failed.")
                        : message,
                    Balance = balance,
                    Price = price,
                };
            }
            catch (Exception ex)
            {
                return new PurchaseResult { Success = false, Message = ex.Message };
            }
        }
    }
}
