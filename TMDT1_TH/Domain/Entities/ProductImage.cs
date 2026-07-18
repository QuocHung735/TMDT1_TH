using TMDT1_TH.Domain.Common;

namespace TMDT1_TH.Domain.Entities;

public class ProductImage : AuditableEntity
{
    public int ProductId { get; set; }
    public int? ProductVariantId { get; set; }
    public string ImageUrl { get; set; } = string.Empty;
    public string? AltText { get; set; }
    public int DisplayOrder { get; set; }
    public bool IsPrimary { get; set; }

    public Product Product { get; set; } = null!;
    public ProductVariant? ProductVariant { get; set; }
}
