namespace SusanooLauncher.Services
{
    internal enum FeatureStatus
    {
        Live,
        Beta,
        Planned,
    }

    internal sealed class LauncherFeature
    {
        public required string Id { get; init; }
        public required string Name { get; init; }
        public required string Category { get; init; }
        public FeatureStatus Status { get; init; }
        public string Description { get; init; } = "";
        public string? NavigateKey { get; init; }
    }

    /// <summary>Catalog of 76 launcher features — Live/Beta have UI; Planned are tracked in the hub.</summary>
    internal static class FeatureRegistry
    {
        public static IReadOnlyList<LauncherFeature> All { get; } = BuildAll();

        public static LauncherFeature? Get(string id) =>
            All.FirstOrDefault(f => f.Id.Equals(id, StringComparison.OrdinalIgnoreCase));

        private static List<LauncherFeature> BuildAll() =>
        [
            // Launcher & UX
            F("friends", "Friends List", "Launcher & UX", FeatureStatus.Live, "Online friends from backend.", "friends"),
            F("party", "Party Lobby", "Launcher & UX", FeatureStatus.Planned, "Removed from launcher — was launcher-only, not in-game."),
            F("news", "News & Patch Notes", "Launcher & UX", FeatureStatus.Live, "MOTD and announcements.", "news"),
            F("notifications", "Notifications", "Launcher & UX", FeatureStatus.Beta, "Shop reset and status toasts.", "news"),
            F("downloads", "Download Manager", "Launcher & UX", FeatureStatus.Live, "Required files on Downloads tab.", "downloads"),
            F("multi-build", "Multiple Build Slots", "Launcher & UX", FeatureStatus.Beta, "Save builds in Settings.", "settings"),
            F("auto-detect-builds", "Auto-Detect Builds", "Launcher & UX", FeatureStatus.Planned, "Scan disk for installs."),
            F("launch-presets", "Launch Presets", "Launcher & UX", FeatureStatus.Planned, "Low memory / windowed args."),
            F("playtime", "Play Time Tracker", "Launcher & UX", FeatureStatus.Live, "Tracks session time.", "stats"),
            F("achievements", "Launcher Achievements", "Launcher & UX", FeatureStatus.Beta, "Badges for login and launch.", "stats"),
            F("themes", "Custom Themes", "Launcher & UX", FeatureStatus.Beta, "Live background + shapes.", "settings"),
            F("shop-timer", "Shop Reset Timer", "Launcher & UX", FeatureStatus.Live, "UTC countdown on Shop.", "shop"),
            F("cosmetic-preview", "Cosmetic Preview", "Launcher & UX", FeatureStatus.Planned, "3D skin preview."),
            F("wishlist", "Shop Wishlist", "Launcher & UX", FeatureStatus.Live, "Star items on Shop page.", "shop"),
            F("shop-purchase", "Buy From Launcher", "Launcher & UX", FeatureStatus.Live, "Purchase cosmetics from the shop.", "shop"),
            F("gift", "Gift Cosmetics", "Launcher & UX", FeatureStatus.Planned, "Gift via MCP."),
            F("locker", "Locker Viewer", "Launcher & UX", FeatureStatus.Beta, "Equipped skin on Profile.", "profile"),
            F("loadout-editor", "Loadout Editor", "Launcher & UX", FeatureStatus.Planned, "Edit before launch."),
            F("server-ping", "Server Ping", "Launcher & UX", FeatureStatus.Beta, "Ping on Servers page.", "servers"),
            F("region-select", "Region Selector", "Launcher & UX", FeatureStatus.Planned, "EU / NA preference."),
            F("maintenance", "Maintenance Mode", "Launcher & UX", FeatureStatus.Beta, "Backend health check.", "news"),
            F("auto-update", "Launcher Auto-Update", "Launcher & UX", FeatureStatus.Live, "Checks GitHub for setup.exe updates."),
            F("crash-upload", "Crash Log Upload", "Launcher & UX", FeatureStatus.Planned, "Send logs to Discord."),
            F("screenshots", "Screenshot Gallery", "Launcher & UX", FeatureStatus.Planned, "Browse captures."),
            F("streamer-mode", "Streamer Mode", "Launcher & UX", FeatureStatus.Live, "Hide email on Profile.", "settings"),

            // Shop & economy
            F("item-shop", "Item Shop", "Shop & Economy", FeatureStatus.Live, "Daily & featured cosmetics.", "shop"),
            F("battle-pass", "Battle Pass", "Shop & Economy", FeatureStatus.Live, "Season pass info.", "battlepass"),
            F("daily-quests", "Daily Quests", "Shop & Economy", FeatureStatus.Live, "Quest overview.", "quests"),
            F("vbucks-balance", "V-Bucks Balance", "Shop & Economy", FeatureStatus.Beta, "Shown on Profile.", "profile"),
            F("refunds", "Refund Timer", "Shop & Economy", FeatureStatus.Planned, "Refund window UI."),
            F("shop-history", "Shop History", "Shop & Economy", FeatureStatus.Planned, "Past rotations."),
            F("sac", "Support A Creator", "Shop & Economy", FeatureStatus.Live, "Creator code in Settings.", "settings"),
            F("bundles", "Item Bundles", "Shop & Economy", FeatureStatus.Planned, "Multi-item offers."),
            F("rarity-colors", "Rarity Colors", "Shop & Economy", FeatureStatus.Live, "Colored shop cards.", "shop"),
            F("shop-admin-preview", "Upcoming Shop", "Shop & Economy", FeatureStatus.Planned, "Staff preview."),

            // Anti-cheat & security
            F("hwid-ban", "HWID Ban Check", "Security", FeatureStatus.Live, "Checked at login & launch."),
            F("ip-ban", "IP Ban Check", "Security", FeatureStatus.Live, "Checked at login, launch & game auth."),
            F("injector-scan", "Injector Detection", "Security", FeatureStatus.Live, "Closes cheats, warns once."),
            F("overlay-scan", "Overlay Detection", "Security", FeatureStatus.Beta, "Arc external overlays."),
            F("driver-scan", "Driver Scan", "Security", FeatureStatus.Planned, "Kernel blocklist."),
            F("integrity", "Game Integrity", "Security", FeatureStatus.Planned, "Hash shipping exe."),
            F("vpn-block", "VPN / Proxy Block", "Security", FeatureStatus.Planned, "Arc AntiProxy."),
            F("report-player", "Report Player", "Security", FeatureStatus.Planned, "In-launcher reports."),
            F("trust-score", "Trust Score", "Security", FeatureStatus.Planned, "Clean player tiers."),
            F("2fa", "Two-Factor Auth", "Security", FeatureStatus.Planned, "Discord 2FA."),
            F("token-refresh", "Secure Tokens", "Security", FeatureStatus.Beta, "Epic tokens via backend."),
            F("audit-log", "Detection Audit Log", "Security", FeatureStatus.Planned, "Discord admin feed."),

            // Social
            F("global-chat", "Global Chat", "Social", FeatureStatus.Live, "Talk to players on the backend.", "chat"),
            F("clans", "Clans / Teams", "Social", FeatureStatus.Planned, "Clan tags."),
            F("player-profiles", "Player Profiles", "Social", FeatureStatus.Beta, "From leaderboard.", "leaderboard"),
            F("recent-players", "Recent Players", "Social", FeatureStatus.Planned, "Last match list."),
            F("block-mute", "Block / Mute", "Social", FeatureStatus.Beta, "Block list on Friends.", "friends"),
            F("discord-rpc", "Discord Rich Presence", "Social", FeatureStatus.Live, "Shows in Discord.", null),
            F("discord-voice", "Discord Voice Status", "Social", FeatureStatus.Planned, "Voice channel hint."),

            // Servers
            F("server-browser", "Server Browser", "Servers", FeatureStatus.Live, "Servers page.", "servers"),
            F("custom-games", "Custom Games", "Servers", FeatureStatus.Planned, "Password lobbies."),
            F("tournaments", "Tournaments", "Servers", FeatureStatus.Beta, "Arena / cup config.", "servers"),
            F("queue-eta", "Queue ETA", "Servers", FeatureStatus.Planned, "Wait time estimate."),
            F("reconnect", "Reconnect Session", "Servers", FeatureStatus.Planned, "Rejoin last match."),
            F("favorite-server", "Favorite Server", "Servers", FeatureStatus.Planned, "Pin a host."),

            // Admin
            F("staff-panel", "Staff Panel", "Admin", FeatureStatus.Planned, "Ban / grant V-Bucks."),
            F("player-graph", "Live Player Graph", "Admin", FeatureStatus.Planned, "Online chart."),
            F("shop-editor", "Shop Editor", "Admin", FeatureStatus.Planned, "Edit catalog_config."),
            F("announce-push", "Push Announcements", "Admin", FeatureStatus.Planned, "Broadcast popup."),
            F("version-gate", "Version Gate", "Admin", FeatureStatus.Planned, "Force launcher update."),

            // Polish
            F("onboarding", "Onboarding Wizard", "Polish", FeatureStatus.Planned, "First-run guide."),
            F("shop-search", "Search Shop", "Polish", FeatureStatus.Live, "Filter by name.", "shop"),
            F("shop-sort", "Sort Shop", "Polish", FeatureStatus.Live, "Sort by price/name.", "shop"),
            F("offline-mode", "Offline Shop Cache", "Polish", FeatureStatus.Beta, "Cached catalog."),
            F("localization", "Localization", "Polish", FeatureStatus.Planned, "Multi-language."),
            F("accessibility", "Accessibility", "Polish", FeatureStatus.Beta, "High contrast setting.", "settings"),
            F("hotkeys", "Keyboard Shortcuts", "Polish", FeatureStatus.Live, "F5 refresh shop.", null),
            F("purchase-sfx", "Purchase Sounds", "Polish", FeatureStatus.Beta, "Navigation sounds.", "settings"),

            // Game
            F("replays", "Replay Browser", "Game", FeatureStatus.Planned, "Local .replay files."),
            F("my-stats", "My Stats", "Game", FeatureStatus.Live, "K/D wins on Stats page.", "stats"),
            F("challenges", "Challenge Tracker", "Game", FeatureStatus.Planned, "Season challenges."),
            F("map-wallpaper", "Map Wallpaper", "Game", FeatureStatus.Live, "Home banner art.", "home"),
            F("music-preview", "Music Pack Preview", "Game", FeatureStatus.Planned, "Preview lobby music."),

            // Growth
            F("premium", "Premium Tier", "Growth", FeatureStatus.Planned, "Supporter perks."),
            F("referrals", "Referral Codes", "Growth", FeatureStatus.Planned, "Invite rewards."),
            F("daily-reward", "Daily Login Reward", "Growth", FeatureStatus.Planned, "Launcher login bonus."),
            F("promo-codes", "Promo Codes", "Growth", FeatureStatus.Planned, "Redeem codes."),
        ];

        private static LauncherFeature F(
            string id, string name, string category, FeatureStatus status, string desc, string? nav = null) =>
            new()
            {
                Id = id,
                Name = name,
                Category = category,
                Status = status,
                Description = desc,
                NavigateKey = nav,
            };
    }
}
