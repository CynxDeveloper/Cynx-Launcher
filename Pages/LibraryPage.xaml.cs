using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using Microsoft.Win32;
using SusanooLauncher.Services;

namespace SusanooLauncher.Pages
{
    public partial class LibraryPage : Page
    {
        private const string DefaultBackendUrl = "http://26.157.83.30:3551";
        private readonly FortniteLaunchService _svc = new FortniteLaunchService();

        public LibraryPage()
        {
            InitializeComponent();
            Loaded += (_, __) => RefreshBuildDisplay();
        }

        private void RefreshBuildDisplay()
        {
            BuildInfo info = BuildInfoService.Resolve(Settings.Default.path);

            BuildNameText.Text = info.DisplayName;
            PathText.Text = string.IsNullOrWhiteSpace(info.BuildPath) ? "(not set)" : info.BuildPath;

            BitmapImage? splash = BuildInfoService.LoadSplashImage(info.SplashPath);
            if (splash != null)
            {
                BuildSplashImage.Source = splash;
                BuildSplashImage.Visibility = Visibility.Visible;
                SplashPlaceholderText.Visibility = Visibility.Collapsed;
            }
            else
            {
                BuildSplashImage.Source = null;
                BuildSplashImage.Visibility = Visibility.Collapsed;
                SplashPlaceholderText.Visibility = Visibility.Visible;
                SplashPlaceholderText.Text = info.IsValid ? "No splash.png" : "No splash";
            }
        }

        private void SetStatus(string text)
        {
            try
            {
                if (!IsLoaded || Dispatcher.HasShutdownStarted)
                    return;

                if (!Dispatcher.CheckAccess())
                {
                    Dispatcher.BeginInvoke(() => SetStatus(text));
                    return;
                }

                StatusText.Text = text;
            }
            catch
            {
                // Page may be unloading while a download is still reporting progress.
            }
        }

        private void SelectFolder(object sender, RoutedEventArgs e)
        {
            OpenFolderDialog openFolderDialog = new OpenFolderDialog
            {
                Title = "Select Fortnite Folder",
                InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86)
            };
            if (openFolderDialog.ShowDialog() == true)
            {
                Settings.Default.path = openFolderDialog.FolderName;
                Settings.Default.Save();
                RefreshBuildDisplay();
            }
        }

        private async void Launch(object sender, RoutedEventArgs e)
        {
            LaunchButton.IsEnabled = false;
            LaunchButton.Content = "Working...";
            SetStatus("Launching...");

            try
            {
                string authLogin = Settings.Default.username;
                string authPassword = Settings.Default.password;
                string authType = "epic";

                bool ok = await _svc.LaunchAsync(
                    basePath: Settings.Default.path,
                    authLogin: authLogin,
                    authPassword: authPassword,
                    authType: authType,
                    backendUrl: string.IsNullOrWhiteSpace(Settings.Default.backend)
                        ? DefaultBackendUrl
                        : Settings.Default.backend,
                    status: SetStatus
                );

                if (!ok)
                {
                    if (!string.IsNullOrWhiteSpace(_svc.LastError))
                        MessageBox.Show(_svc.LastError, "Susanoo Launcher");
                    SetStatus("Ready");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Launch failed unexpectedly:\n\n{ex.Message}",
                    "Susanoo Launcher",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                SetStatus("Ready");
            }
            finally
            {
                LaunchButton.Content = "Launch";
                LaunchButton.IsEnabled = true;
            }
        }
    }
}
