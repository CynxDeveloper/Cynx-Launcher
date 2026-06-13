namespace SusanooLauncher.Services
{
    /// <summary>Target Fortnite build for Susanoo (Chapter 3 Season 1).</summary>
    internal static class GameVersionConstants
    {
        public const double JoinableBuild = 19.10;
        public const int BattlePassSeason = 19;

        public const string SeasonLabel = "Chapter 3 • Season 1";
        public const string SeasonShort = "CHAPTER 3 • SEASON 1";
        public const string Tagline = "Experience the best Chapter 3 Season 1 experience with Susanoo.";

        /// <summary>Epic-style user-agent sent to the backend (must match bVersionJoinable).</summary>
        public const string UserAgent =
            "Fortnite/++Fortnite+Release-19.10-CL-18734220 Windows/19.10";

        public const string Win64BinRelative = "FortniteGame\\Binaries\\Win64";

        public const string ShippingExeRelative = $"{Win64BinRelative}\\FortniteClient-Win64-Shipping.exe";

        public const string LauncherExeRelative = $"{Win64BinRelative}\\FortniteLauncher.exe";

        public const string EacExeRelative = $"{Win64BinRelative}\\FortniteClient-Win64-Shipping_EAC.exe";

        public const string BattleyeExeRelative = $"{Win64BinRelative}\\FortniteClient-Win64-Shipping_BE.exe";
    }
}
