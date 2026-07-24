using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.AspNetCore.Mvc.Rendering;
using TMDT1_TH.Domain.Enums;

namespace TMDT1_TH.Areas.Admin.ViewModels;

public sealed class PromotionIndexViewModel
{
    public IReadOnlyList<PromotionListItem> Items { get; set; }
        = Array.Empty<PromotionListItem>();

    public PromotionFormViewModel Form { get; set; }
        = new();

    public string? Query { get; set; }
    public string? State { get; set; }
    public bool OpenFormModal { get; set; }

    public int ActiveCount { get; set; }
    public int UpcomingCount { get; set; }
    public int ExpiredCount { get; set; }
    public int TotalUsedCount { get; set; }
}

public sealed class PromotionFormViewModel
    : IValidatableObject
{
    public int? Id { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập tên chương trình.")]
    [StringLength(
        200,
        MinimumLength = 3,
        ErrorMessage = "Tên chương trình cần từ 3 đến 200 ký tự.")]
    public string Name { get; set; } = string.Empty;

    [StringLength(50)]
    public string Code { get; set; } = string.Empty;

    [StringLength(
        1000,
        ErrorMessage = "Mô tả tối đa 1000 ký tự.")]
    public string? Description { get; set; }

    [Required]
    public PromotionDiscountType DiscountType { get; set; }
        = PromotionDiscountType.Percentage;

    [Required]
    public PromotionScopeType ScopeType { get; set; }
        = PromotionScopeType.AllProducts;

    [Range(
        typeof(decimal),
        "0.01",
        "999999999999",
        ErrorMessage = "Giá trị giảm phải lớn hơn 0.")]
    public decimal DiscountValue { get; set; }

    [Range(
        typeof(decimal),
        "0.01",
        "999999999999",
        ErrorMessage = "Mức giảm tối đa phải lớn hơn 0.")]
    public decimal? MaximumDiscountAmount { get; set; }

    [Range(
        typeof(decimal),
        "0",
        "999999999999",
        ErrorMessage = "Giá trị đơn tối thiểu không hợp lệ.")]
    public decimal MinimumOrderAmount { get; set; }

    [Range(
        1,
        int.MaxValue,
        ErrorMessage = "Giới hạn lượt dùng phải lớn hơn 0.")]
    public int? UsageLimit { get; set; }

    [Required(ErrorMessage = "Vui lòng chọn thời gian bắt đầu.")]
    public DateTime StartsAt { get; set; } = DateTime.Now;

    [Required(ErrorMessage = "Vui lòng chọn thời gian kết thúc.")]
    public DateTime EndsAt { get; set; }
        = DateTime.Now.AddDays(7);

    public bool IsActive { get; set; } = true;

    public List<int> MarketIds { get; set; } = new();
    public List<int> ProductIds { get; set; } = new();
    public List<int> CategoryIds { get; set; } = new();
    public List<int> BrandIds { get; set; } = new();

    [ValidateNever]
    public IReadOnlyList<SelectListItem> MarketOptions { get; set; }
        = Array.Empty<SelectListItem>();

    [ValidateNever]
    public IReadOnlyList<SelectListItem> ProductOptions { get; set; }
        = Array.Empty<SelectListItem>();

    [ValidateNever]
    public IReadOnlyList<SelectListItem> CategoryOptions { get; set; }
        = Array.Empty<SelectListItem>();

    [ValidateNever]
    public IReadOnlyList<SelectListItem> BrandOptions { get; set; }
        = Array.Empty<SelectListItem>();

    public bool IsEdit => Id.HasValue;

    public IEnumerable<ValidationResult> Validate(
        ValidationContext validationContext)
    {
        if (EndsAt <= StartsAt)
        {
            yield return new ValidationResult(
                "Thời gian kết thúc phải sau thời gian bắt đầu.",
                new[] { nameof(StartsAt), nameof(EndsAt) });
        }

        if (DiscountType ==
                PromotionDiscountType.Percentage &&
            DiscountValue > 100)
        {
            yield return new ValidationResult(
                "Mức giảm phần trăm không được vượt quá 100%.",
                new[] { nameof(DiscountValue) });
        }

        if (DiscountType ==
                PromotionDiscountType.FixedAmount &&
            MaximumDiscountAmount.HasValue)
        {
            yield return new ValidationResult(
                "Giảm theo số tiền không sử dụng mức giảm tối đa.",
                new[] { nameof(MaximumDiscountAmount) });
        }

        if (MarketIds.Count == 0)
        {
            yield return new ValidationResult(
                "Vui lòng chọn ít nhất một thị trường.",
                new[] { nameof(MarketIds) });
        }

        if (ScopeType == PromotionScopeType.Products &&
            ProductIds.Count == 0)
        {
            yield return new ValidationResult(
                "Vui lòng chọn ít nhất một sản phẩm.",
                new[] { nameof(ProductIds) });
        }

        if (ScopeType == PromotionScopeType.Categories &&
            CategoryIds.Count == 0)
        {
            yield return new ValidationResult(
                "Vui lòng chọn ít nhất một danh mục.",
                new[] { nameof(CategoryIds) });
        }

        if (ScopeType == PromotionScopeType.Brands &&
            BrandIds.Count == 0)
        {
            yield return new ValidationResult(
                "Vui lòng chọn ít nhất một thương hiệu.",
                new[] { nameof(BrandIds) });
        }
    }
}

public sealed class PromotionListItem
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string DiscountText { get; set; } = string.Empty;
    public string MinimumOrderText { get; set; } = string.Empty;
    public string ScopeText { get; set; } = string.Empty;
    public string Markets { get; set; } = string.Empty;
    public string UsageText { get; set; } = string.Empty;
    public string Period { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public int UsedCount { get; set; }
}
