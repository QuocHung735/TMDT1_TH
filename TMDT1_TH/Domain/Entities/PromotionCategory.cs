namespace TMDT1_TH.Domain.Entities;

public sealed class PromotionCategory
{
    public int PromotionId { get; set; }
    public int CategoryId { get; set; }

    public Promotion Promotion { get; set; } = null!;
    public Category Category { get; set; } = null!;
}
