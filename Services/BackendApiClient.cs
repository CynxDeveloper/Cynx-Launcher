using System.Net.Http;
using System.Net.Http.Headers;

namespace SusanooLauncher.Services
{
    internal static class BackendApiClient
    {
        public const string FortniteUserAgent =
            GameVersionConstants.UserAgent;

        private static readonly HttpClient _http = CreateClient();

        private static HttpClient CreateClient()
        {
            var client = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
            client.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", FortniteUserAgent);
            return client;
        }

        public static HttpClient Http => _http;

        public static void ApplyAuth(HttpRequestMessage request)
        {
            string? token = UserSession.AccessToken;
            if (string.IsNullOrWhiteSpace(token))
                return;

            string full = token.StartsWith("eg1~", StringComparison.OrdinalIgnoreCase)
                ? token
                : $"eg1~{token}";

            request.Headers.TryAddWithoutValidation("Authorization", $"bearer {full}");
        }

        public static string Url(string path) =>
            $"{UserSession.BackendUrl.TrimEnd('/')}{path}";
    }
}
