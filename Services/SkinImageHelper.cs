using System.IO;
using System.Net.Http;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace SusanooLauncher.Services
{
    internal static class SkinImageHelper
    {
        private static readonly Uri DefaultAvatar = new("pack://application:,,,/Assets/susanoo.png");
        private static readonly HttpClient Http = new() { Timeout = TimeSpan.FromSeconds(12) };

        public static void ApplyTo(Image target, string? iconUrl, string? templateId = null) =>
            _ = ApplyToAsync(target, iconUrl, templateId);

        public static void EnsureCircularClip(Image image, double? diameter = null)
        {
            if (image == null)
                return;

            void UpdateClip()
            {
                double w = diameter ?? image.Width;
                if (w <= 0 || double.IsNaN(w))
                    w = image.ActualWidth;
                double h = diameter ?? image.Height;
                if (h <= 0 || double.IsNaN(h))
                    h = image.ActualHeight;

                if (w <= 0 || h <= 0)
                    return;

                double radius = Math.Min(w, h) / 2;
                image.Clip = new EllipseGeometry(new Point(radius, radius), radius, radius);
            }

            image.SizeChanged -= OnCircularClipSizeChanged;
            image.SizeChanged += OnCircularClipSizeChanged;
            UpdateClip();
        }

        private static void OnCircularClipSizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (sender is Image img)
                EnsureCircularClip(img);
        }

        public static async Task ApplyToAsync(Image target, string? iconUrl, string? templateId = null)
        {
            if (target == null)
                return;

            byte[]? bytes = await DownloadFirstAvailableAsync(iconUrl, templateId);
            await target.Dispatcher.InvokeAsync(() =>
            {
                try
                {
                    if (bytes == null || bytes.Length == 0)
                    {
                        target.Source = new BitmapImage(DefaultAvatar);
                    }
                    else
                    {
                        using var stream = new MemoryStream(bytes);
                        var bitmap = new BitmapImage();
                        bitmap.BeginInit();
                        bitmap.StreamSource = stream;
                        bitmap.CacheOption = BitmapCacheOption.OnLoad;
                        bitmap.EndInit();
                        bitmap.Freeze();
                        target.Source = bitmap;
                    }
                }
                catch
                {
                    target.Source = new BitmapImage(DefaultAvatar);
                }

                EnsureCircularClip(target);
            });
        }

        private static IEnumerable<string> BuildCandidateUrls(string? iconUrl, string? templateId)
        {
            if (!string.IsNullOrWhiteSpace(iconUrl))
                yield return iconUrl;

            string? id = NormalizeCosmeticId(templateId);
            if (string.IsNullOrWhiteSpace(id))
                yield break;

            string slug = id.ToLowerInvariant();
            yield return $"https://fortnite-api.com/images/cosmetics/br/{slug}/icon.png";
            yield return $"https://fortnite-api.com/images/cosmetics/br/{slug}/smallicon.png";
        }

        private static string? NormalizeCosmeticId(string? templateId)
        {
            if (string.IsNullOrWhiteSpace(templateId))
                return null;

            string value = templateId.Trim();
            int colon = value.IndexOf(':');
            return colon >= 0 ? value[(colon + 1)..] : value;
        }

        private static async Task<byte[]?> DownloadFirstAvailableAsync(string? iconUrl, string? templateId)
        {
            foreach (string url in BuildCandidateUrls(iconUrl, templateId))
            {
                try
                {
                    byte[] data = await Http.GetByteArrayAsync(url);
                    if (data.Length > 100)
                        return data;
                }
                catch { }
            }

            string? cosmeticId = NormalizeCosmeticId(templateId);
            if (!string.IsNullOrWhiteSpace(cosmeticId))
            {
                try
                {
                    string apiUrl =
                        $"https://fortnite-api.com/v2/cosmetics/br/search/ids?language=en&id={Uri.EscapeDataString(cosmeticId)}";
                    using HttpResponseMessage resp = await Http.GetAsync(apiUrl);
                    string json = await resp.Content.ReadAsStringAsync();
                    using System.Text.Json.JsonDocument doc = System.Text.Json.JsonDocument.Parse(json);
                    if (doc.RootElement.TryGetProperty("data", out System.Text.Json.JsonElement dataEl) &&
                        dataEl.ValueKind == System.Text.Json.JsonValueKind.Array &&
                        dataEl.GetArrayLength() > 0)
                    {
                        System.Text.Json.JsonElement item = dataEl[0];
                        string? resolved = ExtractIconUrl(item);
                        if (!string.IsNullOrWhiteSpace(resolved))
                        {
                            byte[] img = await Http.GetByteArrayAsync(resolved);
                            if (img.Length > 100)
                                return img;
                        }
                    }
                }
                catch { }
            }

            return null;
        }

        private static string? ExtractIconUrl(System.Text.Json.JsonElement item)
        {
            if (!item.TryGetProperty("images", out System.Text.Json.JsonElement images))
                return null;

            foreach (string key in new[] { "icon", "smallIcon", "featured" })
            {
                if (images.TryGetProperty(key, out System.Text.Json.JsonElement urlEl))
                {
                    string? url = urlEl.GetString();
                    if (!string.IsNullOrWhiteSpace(url))
                        return url;
                }
            }

            return null;
        }
    }
}
