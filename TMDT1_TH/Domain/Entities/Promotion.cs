using TMDT1_TH.Domain.Common;
using TMDT1_TH.Domain.Enums;

namespace TMDT1_TH.Domain.Entities;

public sealed class Promotion : AuditableEntity
{
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string? Description { get; set; }

    public PromotionDiscountType DiscountType { get; set; }
        = PromotionDiscountType.Percentage;

    public PromotionScopeType ScopeType { get; set; }
        = PromotionScopeType.AllProducts;

    public decimal DiscountValue { get; set; }
    public decimal? MaximumDiscountAmount { get; set; }
    public decimal MinimumOrderAmount { get; set; }

    public int? UsageLimit { get; set; }
    public int UsedCount { get; set; }

    public DateTime StartsAt { get; set; } = DateTime.Now;
    public DateTime EndsAt { get; set; } = DateTime.Now.AddDays(7);
    public bool IsActive { get; set; } = true;

    public ICollection<PromotionMarket> Markets { get; set; }
        = new List<PromotionMarket>();

    public ICollection<PromotionProduct> Products { get; set; }
        = new List<PromotionProduct>();

    public ICollection<PromotionCategory> Categories { get; set; }
        = new List<PromotionCategory>();

    public ICollection<PromotionBrand> Brands { get; set; }
        = new List<PromotionBrand>();

    public ICollection<PromotionRedemption> Redemptions { get; set; }
        = new List<PromotionRedemption>();
}



