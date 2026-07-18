using TMDT1_TH.Domain.Common;

namespace TMDT1_TH.Domain.Entities;

public class ProductOptionValue : AuditableEntity
{
    public int ProductOptionId { get; set; }
    public string Value { get; set; } = string.Empty;
    public string? ColorCode { get; set; }
    public int DisplayOrder { get; set; }

    public ProductOption ProductOption { get; set; } = null!;
    public ICollection<ProductVariantValue> VariantValues { get; set; } = new List<ProductVariantValue>();
}
