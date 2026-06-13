namespace SusanooLauncher.Models
{
    public sealed class LeaderboardEntry
    {
        public int Rank { get; set; }
        public string DisplayName { get; set; } = "";
        public string AccountId { get; set; } = "";
        public long HypeArenaPoints { get; set; }
        public long Kills { get; set; }
        public long Wins { get; set; }
        public int Level { get; set; }
        public string SkinTemplateId { get; set; } = "";
        public string SkinName { get; set; } = "";
        public string SkinIconUrl { get; set; } = "";

        public long GetSortValue(string sort) => sort switch
        {
            "kills" => Kills,
            "wins" => Wins,
            _ => HypeArenaPoints,
        };
    }
}
