using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.AspNetCore.Mvc.Rendering;
using TMDT1_TH.Domain.Enums;

namespace TMDT1_TH.Areas.Admin.ViewModels;

public sealed class DashboardViewModel
{
    public IReadOnlyList<MetricCard> Metrics { get; init; } = Array.Empty<MetricCard>();
    public IReadOnlyList<RecentProductRow> RecentProducts { get; init; } = Array.Empty<RecentProductRow>();
    public IReadOnlyList<ActivityRow> Activities { get; init; } = Array.Empty<ActivityRow>();
    public IReadOnlyList<PriceAlertRow> PriceAlerts { get; init; } = Array.Empty<PriceAlertRow>();
    public StockHealthViewModel StockHealth { get; init; } = new();
    public int LowStockCount { get; init; }
    public int UpcomingPriceCount { get; init; }
}

public sealed record MetricCard(string Label, string Value, string Change, string Icon, string Tone, string Caption);
public sealed record RecentProductRow(int Id, string Name, string Sku, string Category, string Price, string Stock, string Status, string Initials, string Tone);
public sealed record ActivityRow(string Icon, string Title, string Description, string Time, string Tone);
public sealed record PriceAlertRow(string Product, string Market, string PriceType, string Day, string Month, string Status);

public sealed class StockHealthViewModel
{
    public int Total { get; init; }
    public int InStock { get; init; }
    public int InStockPercent { get; init; }
    public int LowStock { get; init; }
    public int OutOfStock { get; init; }
    public int ReadyPercent { get; init; }
}

public sealed class ProductsViewModel
{
    public IReadOnlyList<ProductRow> Items { get; init; } = Array.Empty<ProductRow>();
    public IReadOnlyList<SelectListItem> CategoryOptions { get; init; } = Array.Empty<SelectListItem>();
    public IReadOnlyList<SelectListItem> BrandOptions { get; init; } = Array.Empty<SelectListItem>();
    public string? Query { get; init; }
    public int? CategoryId { get; init; }
    public int? BrandId { get; init; }
    public ProductStatus? Status { get; init; }
    public int TotalCount { get; init; }
    public int ActiveCount { get; init; }
    public int DraftCount { get; init; }
    public int OutOfStockCount { get; init; }
}

public sealed record ProductRow(
    int Id,
    string Name,
    string Sku,
    string Category,
    string Brand,
    int VariantCount,
    string Price,
    int Stock,
    string Status,
    string Initials,
    string Tone,
    string? ImageUrl);

public sealed class ProductEditorViewModel : IValidatableObject
{
    public int? Id { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập tên sản phẩm.")]
    [StringLength(250, ErrorMessage = "Tên sản phẩm tối đa 250 ký tự.")]
    public string Name { get; set; } = string.Empty;

    [StringLength(80, ErrorMessage = "Mã sản phẩm tối đa 80 ký tự.")]
    public string Sku { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng chọn danh mục.")]
    [Range(1, int.MaxValue, ErrorMessage = "Vui lòng chọn danh mục.")]
    public int? CategoryId { get; set; }

    [Required(ErrorMessage = "Vui lòng chọn thương hiệu.")]
    [Range(1, int.MaxValue, ErrorMessage = "Vui lòng chọn thương hiệu.")]
    public int? BrandId { get; set; }

    [StringLength(600, ErrorMessage = "Mô tả ngắn tối đa 600 ký tự.")]
    public string? ShortDescription { get; set; }

    public string? Description { get; set; }

    public ProductStatus Status { get; set; } = ProductStatus.Draft;
    public bool IsFeatured { get; set; }
    public bool HasVariants { get; set; } = true;

    [Range(0, int.MaxValue, ErrorMessage = "Tồn kho không được âm.")]
    public int StockQuantity { get; set; }

    [Range(0, double.MaxValue, ErrorMessage = "Khối lượng không được âm.")]
    public decimal? Weight { get; set; }

    [StringLength(100, ErrorMessage = "Tên thuộc tính tối đa 100 ký tự.")]
    public string? OptionName1 { get; set; } = "Dung tích";

    public string? OptionValues1 { get; set; } = "4 lít, 6 lít";

    [StringLength(100, ErrorMessage = "Tên thuộc tính tối đa 100 ký tự.")]
    public string? OptionName2 { get; set; } = "Màu sắc";

    public string? OptionValues2 { get; set; } = "Kem, Xanh mint";

    [Range(0, int.MaxValue, ErrorMessage = "Tồn kho mỗi biến thể không được âm.")]
    public int VariantStockQuantity { get; set; } = 10;

    [Required(ErrorMessage = "Vui lòng chọn thị trường áp dụng giá.")]
    [Range(1, int.MaxValue, ErrorMessage = "Vui lòng chọn thị trường áp dụng giá.")]
    public int? MarketId { get; set; }

    [Range(typeof(decimal), "0", "999999999999", ErrorMessage = "Giá vốn không hợp lệ.")]
    public decimal CostPrice { get; set; }

    [Range(typeof(decimal), "1", "999999999999", ErrorMessage = "Giá niêm yết phải lớn hơn 0.")]
    public decimal ListPrice { get; set; }

    [Range(typeof(decimal), "1", "999999999999", ErrorMessage = "Giá bán phải lớn hơn 0.")]
    public decimal SalePrice { get; set; }

    [Required(ErrorMessage = "Vui lòng chọn thời điểm bắt đầu áp dụng giá.")]
    public DateTime ValidFrom { get; set; } = DateTime.Now;

    public DateTime? ValidTo { get; set; }

    [StringLength(1000, ErrorMessage = "Ghi chú giá tối đa 1000 ký tự.")]
    public string? PriceNote { get; set; }

    public IFormFile? PrimaryImageFile { get; set; }
    public string? ExistingPrimaryImageUrl { get; set; }

    [ValidateNever]
    public IReadOnlyList<SelectListItem> CategoryOptions { get; set; } = Array.Empty<SelectListItem>();

    [ValidateNever]
    public IReadOnlyList<SelectListItem> BrandOptions { get; set; } = Array.Empty<SelectListItem>();

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

        if (ValidTo.HasValue && ValidTo.Value <= ValidFrom)
        {
            yield return new ValidationResult(
                "Thời điểm kết thúc phải sau thời điểm bắt đầu.",
                new[] { nameof(ValidTo), nameof(ValidFrom) });
        }

        if (!HasVariants)
            yield break;

        if (string.IsNullOrWhiteSpace(OptionName1))
        {
            yield return new ValidationResult(
                "Vui lòng nhập tên thuộc tính thứ nhất.",
                new[] { nameof(OptionName1) });
        }

        if (SplitValues(OptionValues1).Count == 0)
        {
            yield return new ValidationResult(
                "Vui lòng nhập ít nhất một giá trị cho thuộc tính thứ nhất.",
                new[] { nameof(OptionValues1) });
        }

        if (!string.IsNullOrWhiteSpace(OptionName2) && SplitValues(OptionValues2).Count == 0)
        {
            yield return new ValidationResult(
                "Thuộc tính thứ hai đã có tên nên cần ít nhất một giá trị.",
                new[] { nameof(OptionValues2) });
        }
    }

    private static IReadOnlyList<string> SplitValues(string? value) =>
        (value ?? string.Empty)
            .Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
}

public sealed class PricingViewModel
{
    public IReadOnlyList<PriceScheduleRow> Items { get; init; } = Array.Empty<PriceScheduleRow>();
    public IReadOnlyList<PriceHistoryRow> History { get; init; } = Array.Empty<PriceHistoryRow>();
}
public sealed record PriceScheduleRow(int Id, string Product, string Variant, string Market, string CostPrice, string ListPrice, string SalePrice, string Period, string Status);
public sealed record PriceHistoryRow(string Product, string Variant, string Market, string PriceType, string OldPrice, string NewPrice, string Change, string User, string Time, string Tone);
