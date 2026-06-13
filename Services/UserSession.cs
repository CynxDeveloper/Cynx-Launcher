namespace SusanooLauncher.Services
{
    internal static class UserSession
    {
        public static string? AccountId { get; private set; }
        public static string? DisplayName { get; private set; }
        public static string? Email { get; private set; }
        public static string? AccessToken { get; private set; }
        public static string? SkinName { get; private set; }
        public static string? SkinIconUrl { get; private set; }
        public static string? SkinTemplateId { get; private set; }

        public static void LoadFromSettings()
        {
            AccountId = Settings.Default.accountId;
            DisplayName = string.IsNullOrWhiteSpace(Settings.Default.displayName)
                ? Settings.Default.username
                : Settings.Default.displayName;
            Email = Settings.Default.username;
            AccessToken = Settings.Default.accessToken;
        }

        public static void ApplyLogin(string? accountId, string? username, string? accessToken)
        {
            AccountId = accountId;
            DisplayName = username;
            Email = Settings.Default.username;
            AccessToken = accessToken;
        }

        public static void ApplySkin(string? skinName, string? skinIconUrl, string? skinTemplateId = null)
        {
            SkinName = skinName;
            SkinIconUrl = skinIconUrl;
            SkinTemplateId = skinTemplateId;
        }

        public static void Clear()
        {
            AccountId = null;
            DisplayName = null;
            Email = null;
            AccessToken = null;
            SkinName = null;
            SkinIconUrl = null;
            SkinTemplateId = null;
        }

        public static string BackendUrl =>
            string.IsNullOrWhiteSpace(Settings.Default.backend)
                ? "http://26.157.83.30:3551"
                : Settings.Default.backend.Trim();
    }
}
