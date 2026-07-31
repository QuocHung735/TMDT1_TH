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
    string? ImageUrl,
    int ListingScore,
    int IssueCount);

public sealed class ProductEditorViewModel
{
    public int? Id { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập tên sản phẩm.")]
    [StringLength(250, MinimumLength = 10, ErrorMessage = "Tên sản phẩm cần từ 10 đến 250 ký tự.")]
    public string Name { get; set; } = string.Empty;

    [StringLength(80, ErrorMessage = "Mã sản phẩm tối đa 80 ký tự.")]
    public string Sku { get; set; } = string.Empty;

    [StringLength(100, ErrorMessage = "Mã model tối đa 100 ký tự.")]
    public string? ModelNumber { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập đơn vị bán.")]
    [StringLength(50, ErrorMessage = "Đơn vị bán tối đa 50 ký tự.")]
    public string Unit { get; set; } = "Cái";

    [Required(ErrorMessage = "Vui lòng chọn danh mục.")]
    [Range(1, int.MaxValue, ErrorMessage = "Vui lòng chọn danh mục.")]
    public int? CategoryId { get; set; }

    [Required(ErrorMessage = "Vui lòng chọn thương hiệu.")]
    [Range(1, int.MaxValue, ErrorMessage = "Vui lòng chọn thương hiệu.")]
    public int? BrandId { get; set; }

    [StringLength(600, ErrorMessage = "Mô tả ngắn tối đa 600 ký tự.")]
    public string? ShortDescription { get; set; }

    public string? Description { get; set; }

    [StringLength(100, ErrorMessage = "Xuất xứ tối đa 100 ký tự.")]
    public string? CountryOfOrigin { get; set; }

    [StringLength(250, ErrorMessage = "Tên nhà sản xuất tối đa 250 ký tự.")]
    public string? ManufacturerName { get; set; }

    [StringLength(500, ErrorMessage = "Địa chỉ nhà sản xuất tối đa 500 ký tự.")]
    public string? ManufacturerAddress { get; set; }

    [Range(0, 120, ErrorMessage = "Thời hạn bảo hành phải từ 0 đến 120 tháng.")]
    public int? WarrantyMonths { get; set; }

    public ProductStatus Status { get; set; } = ProductStatus.Draft;
    public bool IsFeatured { get; set; }
    public bool HasVariants { get; set; }

    [Range(0, int.MaxValue, ErrorMessage = "Tồn kho không được âm.")]
    public int StockQuantity { get; set; }

    [Range(0, int.MaxValue, ErrorMessage = "Ngưỡng cảnh báo tồn kho không được âm.")]
    public int LowStockThreshold { get; set; } = 5;

    [Range(1, 9999, ErrorMessage = "Số lượng mua tối thiểu phải từ 1 đến 9.999.")]
    public int MinPurchaseQuantity { get; set; } = 1;

    [Range(1, 999999, ErrorMessage = "Số lượng mua tối đa không hợp lệ.")]
    public int? MaxPurchaseQuantity { get; set; }

    [Range(typeof(decimal), "0", "999999", ErrorMessage = "Khối lượng không hợp lệ.")]
    public decimal? Weight { get; set; }

    [Range(typeof(decimal), "0", "999999", ErrorMessage = "Chiều dài kiện hàng không hợp lệ.")]
    public decimal? PackageLengthCm { get; set; }

    [Range(typeof(decimal), "0", "999999", ErrorMessage = "Chiều rộng kiện hàng không hợp lệ.")]
    public decimal? PackageWidthCm { get; set; }

    [Range(typeof(decimal), "0", "999999", ErrorMessage = "Chiều cao kiện hàng không hợp lệ.")]
    public decimal? PackageHeightCm { get; set; }

    [StringLength(100, ErrorMessage = "Tên thuộc tính tối đa 100 ký tự.")]
    public string? OptionName1 { get; set; }

    public string? OptionValues1 { get; set; }

    [StringLength(100, ErrorMessage = "Tên thuộc tính tối đa 100 ký tự.")]
    public string? OptionName2 { get; set; }

    public string? OptionValues2 { get; set; }

    public List<ProductVariantEditorItem> Variants { get; set; } = new();

    public int? MarketId { get; set; }

    public List<int> MarketIds { get; set; } = new();

    [Range(typeof(decimal), "0", "999999999999", ErrorMessage = "Giá vốn không hợp lệ.")]
    public decimal CostPrice { get; set; }

    [Range(typeof(decimal), "0", "999999999999", ErrorMessage = "Giá niêm yết không hợp lệ.")]
    public decimal ListPrice { get; set; }

    [Range(typeof(decimal), "0", "999999999999", ErrorMessage = "Giá bán không hợp lệ.")]
    public decimal SalePrice { get; set; }

    public int? ProductPriceScheduleId { get; set; }
    public DateTime ValidFrom { get; set; } = DateTime.Now;
    public DateTime? ValidTo { get; set; }

    [StringLength(1000, ErrorMessage = "Ghi chú giá tối đa 1000 ký tự.")]
    public string? PriceNote { get; set; }

    public List<IFormFile> ImageFiles { get; set; } = new();
    public List<int> RemoveImageIds { get; set; } = new();
    public int? PrimaryImageId { get; set; }

    public List<ProductSpecificationEditorItem> Specifications { get; set; } = new();

    [ValidateNever]
    public IReadOnlyList<ProductImageEditorItem> ExistingImages { get; set; } = Array.Empty<ProductImageEditorItem>();

    [ValidateNever]
    public IReadOnlyList<SelectListItem> CategoryOptions { get; set; } = Array.Empty<SelectListItem>();

    [ValidateNever]
    public IReadOnlyList<SelectListItem> BrandOptions { get; set; } = Array.Empty<SelectListItem>();

    [ValidateNever]
    public IReadOnlyList<SelectListItem> MarketOptions { get; set; } = Array.Empty<SelectListItem>();

    public bool IsEdit => Id.HasValue;
}

public sealed class ProductVariantEditorItem
{
    public int? Id { get; set; }
    public int? PriceScheduleId { get; set; }
    public string CombinationKey { get; set; } = string.Empty;
    public string Value1 { get; set; } = string.Empty;
    public string? Value2 { get; set; }
    public string Name { get; set; } = string.Empty;

    [StringLength(100, ErrorMessage = "SKU biến thể tối đa 100 ký tự.")]
    public string Sku { get; set; } = string.Empty;

    [StringLength(100, ErrorMessage = "Barcode tối đa 100 ký tự.")]
    public string? Barcode { get; set; }

    [Range(0, int.MaxValue, ErrorMessage = "Tồn kho biến thể không được âm.")]
    public int StockQuantity { get; set; }

    [Range(0, int.MaxValue, ErrorMessage = "Ngưỡng cảnh báo không được âm.")]
    public int LowStockThreshold { get; set; } = 5;

    [Range(typeof(decimal), "0", "999999", ErrorMessage = "Khối lượng biến thể không hợp lệ.")]
    public decimal? Weight { get; set; }

    [Range(typeof(decimal), "0", "999999999999", ErrorMessage = "Giá vốn biến thể không hợp lệ.")]
    public decimal CostPrice { get; set; }

    [Range(typeof(decimal), "0", "999999999999", ErrorMessage = "Giá niêm yết biến thể không hợp lệ.")]
    public decimal ListPrice { get; set; }

    [Range(typeof(decimal), "0", "999999999999", ErrorMessage = "Giá bán biến thể không hợp lệ.")]
    public decimal SalePrice { get; set; }

    public bool IsDefault { get; set; }
    public bool IsActive { get; set; } = true;
}

public sealed class ProductImageEditorItem
{
    public int Id { get; init; }
    public string ImageUrl { get; init; } = string.Empty;
    public bool IsPrimary { get; init; }
    public int DisplayOrder { get; init; }
}

public sealed class ProductSpecificationEditorItem
{
    public int? Id { get; set; }

    [StringLength(150, ErrorMessage = "Tên thông số tối đa 150 ký tự.")]
    public string? Name { get; set; }

    [StringLength(1000, ErrorMessage = "Giá trị thông số tối đa 1000 ký tự.")]
    public string? Value { get; set; }
}

public sealed class PricingViewModel
{
    public IReadOnlyList<PriceScheduleRow> Items { get; init; } = Array.Empty<PriceScheduleRow>();
    public IReadOnlyList<PriceHistoryRow> History { get; init; } = Array.Empty<PriceHistoryRow>();
}
public sealed record PriceScheduleRow(int Id, string Product, string Variant, string Market, string CostPrice, string ListPrice, string SalePrice, string Period, string Status);
public sealed record PriceHistoryRow(string Product, string Variant, string Market, string PriceType, string OldPrice, string NewPrice, string Change, string User, string Time, string Tone);

