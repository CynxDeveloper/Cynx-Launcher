using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using SusanooLauncher.Services;

namespace SusanooLauncher.Pages
{
    public partial class ProfilePage : Page
    {
        public ProfilePage()
        {
            InitializeComponent();
            Loaded += async (_, __) => await RefreshAsync();
        }

        private async Task RefreshAsync()
        {
            string name = UserSession.DisplayName ?? "Player";
            NameText.Text = name;

            bool streamer = LocalFeatureStore.Prefs.StreamerMode;
            EmailText.Text = streamer ? "Email hidden (streamer mode)" : (UserSession.Email ?? "");
            AccountIdText.Text = string.IsNullOrWhiteSpace(UserSession.AccountId)
                ? ""
                : $"Account: {UserSession.AccountId}";

            int balance = await ShopPurchaseService.GetBalanceAsync();
            VbucksText.Text = balance >= 0 ? $"V-Bucks: {balance:N0}" : "V-Bucks: —";

            if (string.IsNullOrWhiteSpace(UserSession.SkinIconUrl))
            {
                PlayerSkinInfo skin = await PlayerSkinService.FetchAsync();
                UserSession.ApplySkin(skin.SkinName, skin.SkinIconUrl, skin.SkinTemplateId);
            }

            await SkinAvatar.ApplySkinAsync(UserSession.SkinIconUrl, UserSession.SkinTemplateId);
        }

        private void StatsClicked(object sender, RoutedEventArgs e) =>
            FeatureNavigation.Go("stats");
    }
}
