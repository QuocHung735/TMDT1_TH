using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace TMDT1_TH.Areas.Admin.ViewModels;

public sealed class CategoriesViewModel
{
    public IReadOnlyList<CategoryRow> Items { get; init; } = [];
    public CategoryFormViewModel Form { get; init; } = new();
    public IReadOnlyList<SelectListItem> ParentOptions { get; init; } = [];
    public string? Search { get; init; }
    public bool? Active { get; init; }
    public int TotalCount { get; init; }
    public int ActiveCount { get; init; }
    public int ProductCount { get; init; }
    public bool OpenFormModal { get; init; }
}

public sealed record CategoryRow(
    int Id,
    string Name,
    string Slug,
    string Parent,
    int ProductCount,
    string Status,
    int Level,
    string Icon,
    int DisplayOrder,
    bool IsActive);

public sealed class CategoryFormViewModel
{
    public int? Id { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập tên danh mục.")]
    [StringLength(150, ErrorMessage = "Tên danh mục tối đa 150 ký tự.")]
    [Display(Name = "Tên danh mục")]
    public string Name { get; set; } = string.Empty;

    [StringLength(180, ErrorMessage = "Slug tối đa 180 ký tự.")]
    public string? Slug { get; set; }

    [Display(Name = "Danh mục cha")]
    public int? ParentId { get; set; }

    [StringLength(1000, ErrorMessage = "Mô tả tối đa 1.000 ký tự.")]
    public string? Description { get; set; }

    [StringLength(500, ErrorMessage = "Đường dẫn ảnh tối đa 500 ký tự.")]
    [Display(Name = "Đường dẫn ảnh")]
    public string? ImageUrl { get; set; }

    [Range(0, 9999, ErrorMessage = "Thứ tự hiển thị phải từ 0 đến 9.999.")]
    [Display(Name = "Thứ tự hiển thị")]
    public int DisplayOrder { get; set; }

    [Display(Name = "Đang hiển thị")]
    public bool IsActive { get; set; } = true;
}

public sealed class BrandsViewModel
{
    public IReadOnlyList<BrandRow> Items { get; init; } = [];
    public BrandFormViewModel Form { get; init; } = new();
    public string? Search { get; init; }
    public bool? Active { get; init; }
    public int TotalCount { get; init; }
    public int ActiveCount { get; init; }
    public int ProductCount { get; init; }
    public bool OpenFormModal { get; init; }
}

public sealed record BrandRow(
    int Id,
    string Name,
    string Slug,
    string Country,
    string? WebsiteUrl,
    string? LogoUrl,
    int ProductCount,
    string Status,
    string Initials,
    string Tone,
    bool IsActive);

public sealed class BrandFormViewModel
{
    public int? Id { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập tên thương hiệu.")]
    [StringLength(150, ErrorMessage = "Tên thương hiệu tối đa 150 ký tự.")]
    [Display(Name = "Tên thương hiệu")]
    public string Name { get; set; } = string.Empty;

    [StringLength(180, ErrorMessage = "Slug tối đa 180 ký tự.")]
    public string? Slug { get; set; }

    [StringLength(100, ErrorMessage = "Quốc gia tối đa 100 ký tự.")]
    public string? Country { get; set; }

    [StringLength(300, ErrorMessage = "Website tối đa 300 ký tự.")]
    [Url(ErrorMessage = "Website chưa đúng định dạng URL.")]
    public string? WebsiteUrl { get; set; }

    [StringLength(500, ErrorMessage = "Đường dẫn logo tối đa 500 ký tự.")]
    [Display(Name = "Đường dẫn logo")]
    public string? LogoUrl { get; set; }

    [StringLength(1500, ErrorMessage = "Mô tả tối đa 1.500 ký tự.")]
    public string? Description { get; set; }

    [Display(Name = "Đang hoạt động")]
    public bool IsActive { get; set; } = true;
}
