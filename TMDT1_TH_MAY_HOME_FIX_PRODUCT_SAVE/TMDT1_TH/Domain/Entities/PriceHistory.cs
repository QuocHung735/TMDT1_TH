using TMDT1_TH.Domain.Common;
using TMDT1_TH.Domain.Enums;

namespace TMDT1_TH.Domain.Entities;

public class PriceHistory : AuditableEntity
{
    // Không tạo foreign key để vẫn giữ lịch sử sau khi lịch giá bị xóa.
    public int PriceScheduleId { get; set; }
    public int? ProductId { get; set; }
    public int? ProductVariantId { get; set; }
    public int MarketId { get; set; }

    public decimal? OldCostPrice { get; set; }
    public decimal? NewCostPrice { get; set; }
    public decimal? OldListPrice { get; set; }
    public decimal? NewListPrice { get; set; }
    public decimal? OldSalePrice { get; set; }
    public decimal? NewSalePrice { get; set; }

    public DateTime? OldValidFrom { get; set; }
    public DateTime? NewValidFrom { get; set; }
    public DateTime? OldValidTo { get; set; }
    public DateTime? NewValidTo { get; set; }

    public PriceChangeType Action { get; set; }
    public string ChangedBy { get; set; } = "System";
    public DateTime ChangedAt { get; set; }
    public string? Reason { get; set; }
}
