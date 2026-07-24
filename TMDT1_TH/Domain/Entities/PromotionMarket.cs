namespace TMDT1_TH.Domain.Entities;

public sealed class PromotionMarket
{
    public int PromotionId { get; set; }
    public int MarketId { get; set; }

    public Promotion Promotion { get; set; } = null!;
    public Market Market { get; set; } = null!;
}
