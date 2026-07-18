using TMDT1_TH.Domain.Common;

namespace TMDT1_TH.Domain.Entities;

public class ProductVariant : AuditableEntity
{
    public int ProductId { get; set; }
    public string Sku { get; set; } = string.Empty;
    public string? Barcode { get; set; }
    public string Name { get; set; } = string.Empty;
    // Chuỗi chuẩn hóa tổ hợp, ví dụ: COLOR=WHITE|SIZE=M.
    public string CombinationKey { get; set; } = string.Empty;
    public int StockQuantity { get; set; }
    public decimal? Weight { get; set; }
    public bool IsDefault { get; set; }
    public bool IsActive { get; set; } = true;
    public bool IsDeleted { get; set; }

    public Product Product { get; set; } = null!;
    public ICollection<ProductVariantValue> VariantValues { get; set; } = new List<ProductVariantValue>();
    public ICollection<ProductImage> Images { get; set; } = new List<ProductImage>();
    public ICollection<PriceSchedule> PriceSchedules { get; set; } = new List<PriceSchedule>();
}
