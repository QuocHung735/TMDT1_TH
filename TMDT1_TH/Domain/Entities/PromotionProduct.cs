namespace TMDT1_TH.Domain.Entities;

public sealed class PromotionProduct
{
    public int PromotionId { get; set; }
    public int ProductId { get; set; }

    public Promotion Promotion { get; set; } = null!;
    public Product Product { get; set; } = null!;
}
