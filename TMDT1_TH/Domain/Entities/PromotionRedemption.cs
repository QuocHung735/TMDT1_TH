using TMDT1_TH.Domain.Common;

namespace TMDT1_TH.Domain.Entities;

/// <summary>
/// Lưu một lần sử dụng mã khuyến mãi trên đơn hàng.
/// Bản ghi được giữ lại khi đơn hủy để phục vụ lịch sử và đối soát.
/// </summary>
public sealed class PromotionRedemption : AuditableEntity
{
    public int PromotionId { get; set; }
    public int OrderId { get; set; }
    public int? CustomerUserId { get; set; }

    public string PromotionCode { get; set; } = string.Empty;
    public string PromotionName { get; set; } = string.Empty;
    public decimal DiscountAmount { get; set; }

    public DateTime RedeemedAt { get; set; }

    public bool IsReleased { get; set; }
    public DateTime? ReleasedAt { get; set; }
    public string? ReleaseReason { get; set; }

    public Promotion Promotion { get; set; } = null!;
    public Order Order { get; set; } = null!;
}
