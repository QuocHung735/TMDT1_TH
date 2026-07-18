using TMDT1_TH.Domain.Common;

namespace TMDT1_TH.Domain.Entities;

public class ProductSpecification : AuditableEntity
{
    public int ProductId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public int DisplayOrder { get; set; }

    public Product Product { get; set; } = null!;
}
