using TMDT1_TH.Domain.Common;

namespace TMDT1_TH.Domain.Entities;

public class ProductOption : AuditableEntity
{
    public int ProductId { get; set; }
    public string Name { get; set; } = string.Empty;
    public int DisplayOrder { get; set; }

    public Product Product { get; set; } = null!;
    public ICollection<ProductOptionValue> Values { get; set; } = new List<ProductOptionValue>();
}
