using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace TMDT1_TH.Areas.Admin.ViewModels;

public sealed class CategoryFormViewModel
{
    public int? Id { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập tên danh mục.")]
    [StringLength(150)]
    [Display(Name = "Tên danh mục")]
    public string Name { get; set; } = string.Empty;

    [StringLength(180)]
    public string? Slug { get; set; }

    [StringLength(500)]
    [Display(Name = "Mô tả")]
    public string? Description { get; set; }

    [StringLength(500)]
    [Display(Name = "Đường dẫn ảnh")]
    public string? ImageUrl { get; set; }

    [Range(0, 9999)]
    [Display(Name = "Thứ tự hiển thị")]
    public int DisplayOrder { get; set; }

    [Display(Name = "Danh mục cha")]
    public int? ParentId { get; set; }

    [Display(Name = "Đang hiển thị")]
    public bool IsActive { get; set; } = true;

    public IReadOnlyList<SelectListItem> ParentOptions { get; set; } = [];
}
