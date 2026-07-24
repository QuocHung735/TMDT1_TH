using TMDT1_TH.Domain.Common;

namespace TMDT1_TH.Domain.Entities;

public sealed class OrderItem : AuditableEntity
{
    public int OrderId { get; set; }
    public int? ProductId { get; set; }
    public int? ProductVariantId { get; set; }

    public string ProductName { get; set; } = string.Empty;
    public string? VariantName { get; set; }
    public string Sku { get; set; } = string.Empty;
    public string? ImageUrl { get; set; }
    public string Unit { get; set; } = "Cái";

    public decimal ListPrice { get; set; }
    public decimal UnitPrice { get; set; }
    public int Quantity { get; set; }
    public decimal LineTotal { get; set; }

    public Order Order { get; set; } = null!;
    public Product? Product { get; set; }
    public ProductVariant? ProductVariant { get; set; }
}
