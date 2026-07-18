using System.ComponentModel.DataAnnotations;

namespace TMDT1_TH.Areas.Admin.ViewModels;

public sealed class BrandFormViewModel
{
    public int? Id { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập tên thương hiệu.")]
    [StringLength(150)]
    [Display(Name = "Tên thương hiệu")]
    public string Name { get; set; } = string.Empty;

    [StringLength(180)]
    public string? Slug { get; set; }

    [StringLength(500)]
    [Display(Name = "Logo URL")]
    public string? LogoUrl { get; set; }

    [StringLength(1000)]
    [Display(Name = "Mô tả")]
    public string? Description { get; set; }

    [StringLength(300)]
    [Url(ErrorMessage = "Website không hợp lệ.")]
    public string? WebsiteUrl { get; set; }

    [StringLength(100)]
    [Display(Name = "Quốc gia")]
    public string? Country { get; set; }

    [Display(Name = "Đang hoạt động")]
    public bool IsActive { get; set; } = true;
}
