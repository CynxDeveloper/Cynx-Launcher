using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using SusanooLauncher.Services;

namespace SusanooLauncher.Pages
{
    public partial class DownloadsPage : Page
    {
        public DownloadsPage()
        {
            InitializeComponent();
            RefreshView();
        }

        private void RefreshClicked(object sender, RoutedEventArgs e) => RefreshView();

        private void RefreshView()
        {
            try
            {
                DownloadList.Items.Clear();
                ArcPathText.Text = LauncherDownloads.GetArcExePath();

                string fortniteRoot = Settings.Default.path ?? "";
                if (string.IsNullOrWhiteSpace(fortniteRoot))
                {
                    SummaryText.Text = "Select a Fortnite folder in Library first.";
                    return;
                }

                int total = 0;
                int present = 0;

                foreach (LauncherDownloads.DownloadEntry entry in LauncherDownloads.BuildManifest(fortniteRoot))
                {
                    total++;
                    bool exists = false;
                    try
                    {
                        exists = File.Exists(entry.TargetPath);
                    }
                    catch
                    {
                        // Invalid path characters in saved folder setting.
                    }

                    if (exists) present++;

                    string source = string.IsNullOrWhiteSpace(entry.SourceUrl) ? "" : $"  <-  {entry.SourceUrl}";
                    string state = exists ? "OK" : (entry.Optional ? "OPTIONAL" : "MISSING");
                    DownloadList.Items.Add($"{state}  {entry.Label}{source}");
                }

                SummaryText.Text = $"{present}/{total} files present";
            }
            catch (Exception ex)
            {
                SummaryText.Text = "Could not refresh download list.";
                DownloadList.Items.Add(ex.Message);
            }
        }
    }
}
