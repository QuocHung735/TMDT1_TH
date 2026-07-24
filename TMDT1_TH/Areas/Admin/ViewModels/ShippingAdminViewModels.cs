using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace TMDT1_TH.Areas.Admin.ViewModels;

public sealed class ShippingManagementViewModel
{
    public string? Query { get; init; }
    public bool? Active { get; init; }

    public int CarrierCount { get; init; }
    public int ActiveCarrierCount { get; init; }
    public int ServiceCount { get; init; }
    public int ActiveServiceCount { get; init; }

    public ShippingCarrierFormViewModel CarrierForm { get; init; }
        = new();

    public ShippingServiceFormViewModel ServiceForm { get; init; }
        = new();

    public bool OpenCarrierForm { get; init; }
    public bool OpenServiceForm { get; init; }

    public IReadOnlyList<ShippingCarrierRowViewModel> Carriers { get; init; }
        = Array.Empty<ShippingCarrierRowViewModel>();

    public IReadOnlyList<ShippingServiceRowViewModel> Services { get; init; }
        = Array.Empty<ShippingServiceRowViewModel>();

    public IReadOnlyList<SelectListItem> CarrierOptions { get; init; }
        = Array.Empty<SelectListItem>();
}

public sealed class ShippingCarrierFormViewModel
{
    public int? Id { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập mã đơn vị giao nhận.")]
    [StringLength(30)]
    [RegularExpression(
        @"^[A-Za-z0-9_-]+$",
        ErrorMessage = "Mã chỉ gồm chữ, số, dấu gạch ngang hoặc gạch dưới.")]
    [Display(Name = "Mã đơn vị (tự động)")]
    public string Code { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập tên đơn vị giao nhận.")]
    [StringLength(150)]
    [Display(Name = "Tên đơn vị")]
    public string Name { get; set; } = string.Empty;

    [StringLength(30)]
    [Display(Name = "Số điện thoại")]
    public string? PhoneNumber { get; set; }

    [StringLength(500)]
    [Url(ErrorMessage = "Website chưa đúng định dạng URL.")]
    [Display(Name = "Website")]
    public string? WebsiteUrl { get; set; }

    [StringLength(700)]
    [Display(Name = "Mẫu URL tra cứu")]
    public string? TrackingUrlTemplate { get; set; }

    [Range(0, 9999)]
    [Display(Name = "Thứ tự")]
    public int DisplayOrder { get; set; }

    [Display(Name = "Đang hoạt động")]
    public bool IsActive { get; set; } = true;

    public bool IsEdit => Id.HasValue;
}

public sealed class ShippingServiceFormViewModel
{
    public int? Id { get; set; }

    [Required(ErrorMessage = "Vui lòng chọn đơn vị giao nhận.")]
    [Range(1, int.MaxValue, ErrorMessage = "Vui lòng chọn đơn vị giao nhận.")]
    [Display(Name = "Đơn vị giao nhận")]
    public int? ShippingCarrierId { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập mã dịch vụ.")]
    [StringLength(30)]
    [RegularExpression(
        @"^[A-Za-z0-9_-]+$",
        ErrorMessage = "Mã chỉ gồm chữ, số, dấu gạch ngang hoặc gạch dưới.")]
    [Display(Name = "Mã dịch vụ (tự động)")]
    public string Code { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập tên dịch vụ.")]
    [StringLength(150)]
    [Display(Name = "Tên dịch vụ")]
    public string Name { get; set; } = string.Empty;

    [StringLength(500)]
    [Display(Name = "Mô tả")]
    public string? Description { get; set; }

    [Range(
        typeof(decimal),
        "0",
        "999999999",
        ErrorMessage = "Phí vận chuyển chưa hợp lệ.")]
    [Display(Name = "Phí cơ bản")]
    public decimal BaseFee { get; set; }

    [Range(0, 60)]
    [Display(Name = "Số ngày tối thiểu")]
    public int EstimatedMinDays { get; set; } = 1;

    [Range(0, 60)]
    [Display(Name = "Số ngày tối đa")]
    public int EstimatedMaxDays { get; set; } = 3;

    [Range(0, 9999)]
    [Display(Name = "Thứ tự")]
    public int DisplayOrder { get; set; }

    [Display(Name = "Đang hoạt động")]
    public bool IsActive { get; set; } = true;

    public bool IsEdit => Id.HasValue;
}

public sealed class ShippingCarrierRowViewModel
{
    public int Id { get; init; }
    public string Code { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string? PhoneNumber { get; init; }
    public string? WebsiteUrl { get; init; }
    public string? TrackingUrlTemplate { get; init; }
    public int ServiceCount { get; init; }
    public int ActiveServiceCount { get; init; }
    public bool IsActive { get; init; }
    public int DisplayOrder { get; init; }
}

public sealed class ShippingServiceRowViewModel
{
    public int Id { get; init; }
    public int ShippingCarrierId { get; init; }
    public string CarrierName { get; init; } = string.Empty;
    public string Code { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public decimal BaseFee { get; init; }
    public int EstimatedMinDays { get; init; }
    public int EstimatedMaxDays { get; init; }
    public int OrderCount { get; init; }
    public bool IsActive { get; init; }
    public bool CarrierIsActive { get; init; }
    public int DisplayOrder { get; init; }
}
