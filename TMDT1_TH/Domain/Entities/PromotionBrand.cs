namespace TMDT1_TH.Domain.Entities;

public sealed class PromotionBrand
{
    public int PromotionId { get; set; }
    public int BrandId { get; set; }

    public Promotion Promotion { get; set; } = null!;
    public Brand Brand { get; set; } = null!;
}
