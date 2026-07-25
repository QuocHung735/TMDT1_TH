using System.ComponentModel.DataAnnotations;

namespace TMDT1_TH.Areas.Admin.ViewModels;

public sealed class StoresViewModel
{
    public IReadOnlyList<StoreRow> Items { get; init; }
        = Array.Empty<StoreRow>();

    public StoreFormViewModel Form { get; init; } = new();

    public string? Query { get; init; }
    public bool? Active { get; init; }

    public int TotalCount { get; init; }
    public int ActiveCount { get; init; }
    public int VerifiedCount { get; init; }
    public int ProductCount { get; init; }
}

public sealed class StoreFormViewModel
{
    public int? Id { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập tên cửa hàng.")]
    [StringLength(
        200,
        MinimumLength = 3,
        ErrorMessage =
            "Tên cửa hàng cần từ 3 đến 200 ký tự.")]
    public string Name { get; set; } = string.Empty;

    [StringLength(
        220,
        ErrorMessage = "Đường dẫn tối đa 220 ký tự.")]
    public string? Slug { get; set; }

    [StringLength(
        1200,
        ErrorMessage = "Mô tả tối đa 1.200 ký tự.")]
    public string? Description { get; set; }

    [StringLength(
        500,
        ErrorMessage = "Đường dẫn logo tối đa 500 ký tự.")]
    public string? LogoUrl { get; set; }

    [EmailAddress(
        ErrorMessage = "Email liên hệ chưa hợp lệ.")]
    [StringLength(256)]
    public string? ContactEmail { get; set; }

    [Phone(
        ErrorMessage = "Số điện thoại chưa hợp lệ.")]
    [StringLength(30)]
    public string? PhoneNumber { get; set; }

    [StringLength(400)]
    public string? AddressLine { get; set; }

    [StringLength(150)]
    public string? Ward { get; set; }

    [StringLength(150)]
    public string? District { get; set; }

    [StringLength(150)]
    public string? Province { get; set; }

    [Range(
        0,
        9999,
        ErrorMessage =
            "Thứ tự hiển thị phải từ 0 đến 9.999.")]
    public int DisplayOrder { get; set; }

    public bool IsActive { get; set; } = true;
    public bool IsVerified { get; set; }

    public bool IsEdit => Id.HasValue;
}

public sealed record StoreRow(
    int Id,
    string Name,
    string Slug,
    string Location,
    string Contact,
    int ProductCount,
    bool IsActive,
    bool IsVerified,
    decimal? ReliabilityScore,
    string ReliabilityLabel,
    int DisplayOrder,
    bool CanDelete);
