using System.ComponentModel.DataAnnotations;

namespace TMDT1_TH.Areas.Admin.ViewModels;

public sealed class MarketsViewModel
{
    public IReadOnlyList<MarketRow> Items { get; init; } = Array.Empty<MarketRow>();
    public MarketFormViewModel Form { get; init; } = new();
    public string? Search { get; init; }
    public bool? Active { get; init; }
    public int TotalCount { get; init; }
    public int ActiveCount { get; init; }
    public int TotalPriceCount { get; init; }
    public bool OpenFormModal { get; init; }
}

public sealed record MarketRow(
    int Id,
    string Code,
    string Name,
    string Currency,
    string CountryCode,
    string? Description,
    int PriceCount,
    int ActivePriceCount,
    string Status,
    bool IsDefault,
    bool IsActive);

public sealed class MarketFormViewModel : IValidatableObject
{
    public int? Id { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập mã thị trường.")]
    [StringLength(30, MinimumLength = 2, ErrorMessage = "Mã thị trường phải từ 2 đến 30 ký tự.")]
    [RegularExpression("^[A-Za-z0-9-]+$", ErrorMessage = "Mã thị trường chỉ gồm chữ, số và dấu gạch ngang.")]
    [Display(Name = "Mã thị trường")]
    public string Code { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập tên thị trường.")]
    [StringLength(150, ErrorMessage = "Tên thị trường tối đa 150 ký tự.")]
    [Display(Name = "Tên thị trường")]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập mã tiền tệ.")]
    [StringLength(10, MinimumLength = 3, ErrorMessage = "Mã tiền tệ phải từ 3 đến 10 ký tự.")]
    [RegularExpression("^[A-Za-z0-9-]+$", ErrorMessage = "Mã tiền tệ chỉ gồm chữ, số và dấu gạch ngang.")]
    [Display(Name = "Tiền tệ")]
    public string CurrencyCode { get; set; } = "VND";

    [StringLength(10, ErrorMessage = "Mã quốc gia tối đa 10 ký tự.")]
    [RegularExpression("^[A-Za-z0-9-]*$", ErrorMessage = "Mã quốc gia chỉ gồm chữ, số và dấu gạch ngang.")]
    [Display(Name = "Mã quốc gia")]
    public string? CountryCode { get; set; } = "VN";

    [StringLength(500, ErrorMessage = "Mô tả tối đa 500 ký tự.")]
    public string? Description { get; set; }

    [Display(Name = "Đang hoạt động")]
    public bool IsActive { get; set; } = true;

    [Display(Name = "Thị trường mặc định")]
    public bool IsDefault { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (IsDefault && !IsActive)
        {
            yield return new ValidationResult(
                "Thị trường mặc định phải ở trạng thái đang hoạt động.",
                new[] { nameof(IsDefault), nameof(IsActive) });
        }
    }
}
