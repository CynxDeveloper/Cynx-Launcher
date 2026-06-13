using System.Windows.Controls;
using SusanooLauncher.Services;

namespace SusanooLauncher.Pages
{
    public partial class BattlePassPage : Page
    {
        public BattlePassPage()
        {
            InitializeComponent();
            SeasonText.Text = $"{GameVersionConstants.SeasonLabel} Battle Pass";
            LevelText.Text = "Open Fortnite to view your tier progress.";
        }
    }
}
