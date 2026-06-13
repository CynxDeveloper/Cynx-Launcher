using System.Windows;
using System.Windows.Controls;
using SusanooLauncher.Services;

namespace SusanooLauncher.Pages
{
    public partial class LoginPage : Page
    {
        private const string DefaultBackendUrl = "http://26.157.83.30:3551";
        private bool _registerMode;
        private bool _busy;

        public event EventHandler? LoggedIn;

        public LoginPage()
        {
            InitializeComponent();
            Username.Text = Settings.Default.username;
            Password.Password = Settings.Default.password;
        }

        private void ToggleModeClicked(object sender, RoutedEventArgs e)
        {
            _registerMode = !_registerMode;
            ApplyModeUi();
            ClearError();
        }

        private void ApplyModeUi()
        {
            if (_registerMode)
            {
                SubtitleText.Text = "Create your Susanoo account";
                ContinueBtn.Content = "Register";
                ToggleModeBtn.Content = "Already have an account? Sign in";
                RegisterUsernameLabel.Visibility = Visibility.Visible;
                RegisterUsername.Visibility = Visibility.Visible;
                DiscordIdLabel.Visibility = Visibility.Visible;
                DiscordIdBox.Visibility = Visibility.Visible;
                DiscordIdHint.Visibility = Visibility.Visible;
            }
            else
            {
                SubtitleText.Text = "Sign in to continue";
                ContinueBtn.Content = "Continue";
                ToggleModeBtn.Content = "Create an account";
                RegisterUsernameLabel.Visibility = Visibility.Collapsed;
                RegisterUsername.Visibility = Visibility.Collapsed;
                DiscordIdLabel.Visibility = Visibility.Collapsed;
                DiscordIdBox.Visibility = Visibility.Collapsed;
                DiscordIdHint.Visibility = Visibility.Collapsed;
            }
        }

        private async void ContinueClicked(object sender, RoutedEventArgs e)
        {
            if (_busy)
                return;

            string email = (Username.Text ?? "").Trim();
            string pass = Password.Password ?? "";

            if (string.IsNullOrWhiteSpace(email))
            {
                ShowError("Please enter your email.");
                return;
            }

            if (string.IsNullOrWhiteSpace(pass))
            {
                ShowError("Please enter your password.");
                return;
            }

            string backend = string.IsNullOrWhiteSpace(Settings.Default.backend)
                ? DefaultBackendUrl
                : Settings.Default.backend;

            SetBusy(true);
            ClearError();

            LauncherAuthResult result;
            if (_registerMode)
            {
                string registerName = (RegisterUsername.Text ?? "").Trim();
                string discordId = (DiscordIdBox.Text ?? "").Trim();
                if (string.IsNullOrWhiteSpace(registerName))
                {
                    ShowError("Please enter a username.");
                    SetBusy(false);
                    return;
                }

                if (string.IsNullOrWhiteSpace(discordId) || discordId.Length < 17 || !discordId.All(char.IsDigit))
                {
                    ShowError("Please enter your Discord User ID (Developer Mode → right-click profile → Copy User ID).");
                    SetBusy(false);
                    return;
                }

                string hwid = HardwareIdService.GetMachineId();
                result = await LauncherAuthService.RegisterAsync(backend, email, pass, registerName, discordId, hwid);
            }
            else
            {
                result = await LauncherAuthService.LoginAsync(
                    backend,
                    email,
                    pass,
                    HardwareIdService.GetMachineId());
            }

            SetBusy(false);

            if (!result.Success)
            {
                if (result.IpBanned)
                {
                    System.Windows.MessageBox.Show(
                        result.Message,
                        "Susanoo Launcher",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                }

                ShowError(result.Message);
                if (result.HwidLocked || result.AlreadyRegistered)
                    ToggleModeBtn.Content = "Already have an account? Sign in";
                else if (result.NotRegistered && !_registerMode)
                    ToggleModeBtn.Content = "No account? Register here";
                return;
            }

            if (_registerMode && !string.IsNullOrWhiteSpace(result.Message))
            {
                System.Windows.MessageBox.Show(
                    result.Message,
                    "Susanoo Launcher",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }

            Settings.Default.username = email;
            Settings.Default.password = pass;
            Settings.Default.accessToken = result.AccessToken ?? "";
            Settings.Default.accountId = result.AccountId ?? "";
            Settings.Default.displayName = result.Username ?? email;
            Settings.Default.Save();

            UserSession.ApplyLogin(result.AccountId, result.Username ?? email, result.AccessToken);
            UserSession.ApplySkin(result.SkinName, result.SkinIconUrl, result.SkinTemplateId);
            LocalFeatureStore.UnlockAchievement("first_login");

            LoggedIn?.Invoke(this, EventArgs.Empty);
        }

        private void SetBusy(bool busy)
        {
            _busy = busy;
            ContinueBtn.IsEnabled = !busy;
            ToggleModeBtn.IsEnabled = !busy;
            Username.IsEnabled = !busy;
            Password.IsEnabled = !busy;
            RegisterUsername.IsEnabled = !busy;
            DiscordIdBox.IsEnabled = !busy;
            ContinueBtn.Content = busy ? "Please wait..." : (_registerMode ? "Register" : "Continue");
        }

        private void ShowError(string message)
        {
            ErrorText.Text = message;
            ErrorText.Visibility = Visibility.Visible;
        }

        private void ClearError()
        {
            ErrorText.Text = "";
            ErrorText.Visibility = Visibility.Collapsed;
        }
    }
}
