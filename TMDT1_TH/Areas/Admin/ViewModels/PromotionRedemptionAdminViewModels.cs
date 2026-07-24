namespace TMDT1_TH.Areas.Admin.ViewModels;

public sealed class PromotionRedemptionHistoryViewModel
{
    public string? Query { get; set; }
    public string? State { get; set; }

    public int TotalCount { get; set; }
    public int ActiveCount { get; set; }
    public int ReleasedCount { get; set; }

    public decimal TotalDiscountAmount { get; set; }
    public decimal ReleasedDiscountAmount { get; set; }

    public IReadOnlyList<PromotionRedemptionListItem>
        Items { get; set; }
        = Array.Empty<PromotionRedemptionListItem>();
}

public sealed class PromotionRedemptionListItem
{
    public int Id { get; set; }

    public string PromotionCode { get; set; }
        = string.Empty;

    public string PromotionName { get; set; }
        = string.Empty;

    public int OrderId { get; set; }

    public string OrderNumber { get; set; }
        = string.Empty;

    public string CustomerName { get; set; }
        = string.Empty;

    public string? CustomerEmail { get; set; }

    public decimal DiscountAmount { get; set; }
    public string CurrencyCode { get; set; } = "VND";

    public DateTime RedeemedAt { get; set; }

    public bool IsReleased { get; set; }
    public DateTime? ReleasedAt { get; set; }
    public string? ReleaseReason { get; set; }
}
