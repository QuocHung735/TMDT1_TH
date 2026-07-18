using TMDT1_TH.Domain.Common;

namespace TMDT1_TH.Domain.Entities;

public class PriceSchedule : AuditableEntity
{
    public int? ProductId { get; set; }
    public int? ProductVariantId { get; set; }
    public int MarketId { get; set; }

    // Ba giá bắt buộc của sản phẩm/biến thể.
    public decimal CostPrice { get; set; }
    public decimal ListPrice { get; set; }
    public decimal SalePrice { get; set; }

    public DateTime ValidFrom { get; set; }
    public DateTime? ValidTo { get; set; } // null = áp dụng vô hạn
    public bool IsActive { get; set; } = true;
    public string? Note { get; set; }

    public Product? Product { get; set; }
    public ProductVariant? ProductVariant { get; set; }
    public Market Market { get; set; } = null!;
}
