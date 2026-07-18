using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace TMDT1_TH.Areas.Admin.ViewModels;

public sealed class PricingIndexViewModel
{
    public IReadOnlyList<PricingScheduleListItem> Items { get; set; } = Array.Empty<PricingScheduleListItem>();
    public IReadOnlyList<PricingHistoryListItem> History { get; set; } = Array.Empty<PricingHistoryListItem>();
    public IReadOnlyList<SelectListItem> MarketFilterOptions { get; set; } = Array.Empty<SelectListItem>();

    public PricingScheduleFormViewModel Form { get; set; } = new PricingScheduleFormViewModel();

    public string? Query { get; set; }
    public int? MarketId { get; set; }
    public string? State { get; set; }
    public bool OpenFormModal { get; set; }

    public int CurrentCount { get; set; }
    public int UpcomingCount { get; set; }
    public int ExpiringCount { get; set; }
    public int ChangedThisMonthCount { get; set; }
    public int ActiveMarketCount { get; set; }
}

public sealed class PricingScheduleFormViewModel : IValidatableObject
{
    public int? Id { get; set; }

    [Required(ErrorMessage = "Vui lòng chọn sản phẩm hoặc biến thể.")]
    public string TargetKey { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng chọn thị trường.")]
    [Range(1, int.MaxValue, ErrorMessage = "Vui lòng chọn thị trường.")]
    public int? MarketId { get; set; }

    [Range(typeof(decimal), "0", "999999999999", ErrorMessage = "Giá vốn không hợp lệ.")]
    public decimal CostPrice { get; set; }

    [Range(typeof(decimal), "1", "999999999999", ErrorMessage = "Giá niêm yết phải lớn hơn 0.")]
    public decimal ListPrice { get; set; }

    [Range(typeof(decimal), "1", "999999999999", ErrorMessage = "Giá bán phải lớn hơn 0.")]
    public decimal SalePrice { get; set; }

    [Required(ErrorMessage = "Vui lòng chọn thời điểm bắt đầu.")]
    public DateTime ValidFrom { get; set; } = DateTime.Now;

    public DateTime? ValidTo { get; set; }
    public bool IsUnlimited { get; set; } = true;
    public bool IsActive { get; set; } = true;

    [StringLength(1000, ErrorMessage = "Ghi chú tối đa 1000 ký tự.")]
    public string? Note { get; set; }

    [ValidateNever]
    public IReadOnlyList<SelectListItem> TargetOptions { get; set; } = Array.Empty<SelectListItem>();

    [ValidateNever]
    public IReadOnlyList<SelectListItem> MarketOptions { get; set; } = Array.Empty<SelectListItem>();

    public bool IsEdit => Id.HasValue;

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (SalePrice > ListPrice)
        {
            yield return new ValidationResult(
                "Giá bán không được lớn hơn giá niêm yết.",
                new[] { nameof(SalePrice), nameof(ListPrice) });
        }

        if (!IsUnlimited && !ValidTo.HasValue)
        {
            yield return new ValidationResult(
                "Vui lòng chọn thời điểm kết thúc hoặc bật áp dụng vô hạn.",
                new[] { nameof(ValidTo) });
        }

        if (!IsUnlimited && ValidTo.HasValue && ValidTo.Value <= ValidFrom)
        {
            yield return new ValidationResult(
                "Thời điểm kết thúc phải sau thời điểm bắt đầu.",
                new[] { nameof(ValidTo), nameof(ValidFrom) });
        }
    }
}

public sealed class PricingScheduleListItem
{
    public int Id { get; set; }
    public string Product { get; set; } = string.Empty;
    public string Variant { get; set; } = string.Empty;
    public string Sku { get; set; } = string.Empty;
    public string Market { get; set; } = string.Empty;
    public string CostPrice { get; set; } = string.Empty;
    public string ListPrice { get; set; } = string.Empty;
    public string SalePrice { get; set; } = string.Empty;
    public string Period { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}

public sealed class PricingHistoryListItem
{
    public string Product { get; set; } = string.Empty;
    public string Variant { get; set; } = string.Empty;
    public string Market { get; set; } = string.Empty;
    public string ChangeType { get; set; } = string.Empty;
    public string OldValue { get; set; } = string.Empty;
    public string NewValue { get; set; } = string.Empty;
    public string Change { get; set; } = string.Empty;
    public string User { get; set; } = string.Empty;
    public string Time { get; set; } = string.Empty;
    public string Tone { get; set; } = "neutral";
    public string Action { get; set; } = string.Empty;
    public string? Reason { get; set; }
}
