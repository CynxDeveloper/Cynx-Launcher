namespace SusanooLauncher.Models
{
    public sealed class ShopItem
    {
        public string Name { get; init; } = "";
        public int Price { get; init; }
        public string ImageUrl { get; init; } = "";
        public string Section { get; init; } = "Daily";
        public string TemplateId { get; init; } = "";
        public string CosmeticId { get; init; } = "";
        public string OfferId { get; init; } = "";
        public string Rarity { get; init; } = "common";
    }
}
