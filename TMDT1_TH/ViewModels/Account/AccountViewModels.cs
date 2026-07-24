using System.ComponentModel.DataAnnotations;

namespace TMDT1_TH.ViewModels.Account;

public sealed class RegisterViewModel
{
    [Required(ErrorMessage = "Vui lòng nhập họ tên.")]
    [StringLength(
        150,
        MinimumLength = 2,
        ErrorMessage = "Họ tên cần từ 2 đến 150 ký tự.")]
    [Display(Name = "Họ và tên")]
    public string FullName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập email.")]
    [EmailAddress(ErrorMessage = "Email chưa đúng định dạng.")]
    [StringLength(180)]
    [Display(Name = "Email")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập số điện thoại.")]
    [StringLength(30, MinimumLength = 9)]
    [RegularExpression(
        @"^[0-9+\s().-]{9,30}$",
        ErrorMessage = "Số điện thoại chưa hợp lệ.")]
    [Display(Name = "Số điện thoại")]
    public string PhoneNumber { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập mật khẩu.")]
    [DataType(DataType.Password)]
    [StringLength(
        100,
        MinimumLength = 8,
        ErrorMessage = "Mật khẩu phải có ít nhất 8 ký tự.")]
    [Display(Name = "Mật khẩu")]
    public string Password { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng xác nhận mật khẩu.")]
    [DataType(DataType.Password)]
    [Compare(
        nameof(Password),
        ErrorMessage = "Mật khẩu xác nhận không khớp.")]
    [Display(Name = "Xác nhận mật khẩu")]
    public string ConfirmPassword { get; set; } = string.Empty;

    public string? ReturnUrl { get; set; }
}

public sealed class LoginViewModel
{
    [Required(ErrorMessage = "Vui lòng nhập email.")]
    [EmailAddress(ErrorMessage = "Email chưa đúng định dạng.")]
    [Display(Name = "Email")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập mật khẩu.")]
    [DataType(DataType.Password)]
    [Display(Name = "Mật khẩu")]
    public string Password { get; set; } = string.Empty;

    [Display(Name = "Ghi nhớ đăng nhập")]
    public bool RememberMe { get; set; }

    public string? ReturnUrl { get; set; }
}

public sealed class ChangePasswordViewModel
{
    [Required(ErrorMessage = "Vui lòng nhập mật khẩu hiện tại.")]
    [DataType(DataType.Password)]
    [Display(Name = "Mật khẩu hiện tại")]
    public string CurrentPassword { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập mật khẩu mới.")]
    [DataType(DataType.Password)]
    [StringLength(100, MinimumLength = 8)]
    [Display(Name = "Mật khẩu mới")]
    public string NewPassword { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng xác nhận mật khẩu mới.")]
    [DataType(DataType.Password)]
    [Compare(
        nameof(NewPassword),
        ErrorMessage = "Mật khẩu xác nhận không khớp.")]
    [Display(Name = "Xác nhận mật khẩu mới")]
    public string ConfirmPassword { get; set; } = string.Empty;
}

public sealed class ProfileViewModel
{
    [Required(ErrorMessage = "Vui lòng nhập họ tên.")]
    [StringLength(
        150,
        MinimumLength = 2,
        ErrorMessage = "Họ tên cần từ 2 đến 150 ký tự.")]
    [Display(Name = "Họ và tên")]
    public string FullName { get; set; } = string.Empty;

    [Display(Name = "Email")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập số điện thoại.")]
    [StringLength(30, MinimumLength = 9)]
    [RegularExpression(
        @"^[0-9+\s().-]{9,30}$",
        ErrorMessage = "Số điện thoại chưa hợp lệ.")]
    [Display(Name = "Số điện thoại")]
    public string PhoneNumber { get; set; } = string.Empty;

    [DataType(DataType.Date)]
    [Display(Name = "Ngày sinh")]
    public DateTime? DateOfBirth { get; set; }

    [StringLength(20)]
    [Display(Name = "Giới tính")]
    public string? Gender { get; set; }

    [StringLength(150)]
    [Display(Name = "Tỉnh/Thành phố")]
    public string? Province { get; set; }

    [StringLength(150)]
    [Display(Name = "Quận/Huyện")]
    public string? District { get; set; }

    [StringLength(150)]
    [Display(Name = "Phường/Xã")]
    public string? Ward { get; set; }

    [StringLength(500)]
    [Display(Name = "Số nhà và tên đường")]
    public string? AddressLine { get; set; }

    public DateTime CreatedAt { get; set; }
    public string Initials { get; set; } = "KH";
    public string RoleName { get; set; } = "Khách hàng";

    public int TotalOrders { get; set; }
    public int ActiveOrders { get; set; }
    public int CompletedOrders { get; set; }
    public int TotalReviews { get; set; }

    public bool HasDefaultAddress =>
        !string.IsNullOrWhiteSpace(Province) &&
        !string.IsNullOrWhiteSpace(District) &&
        !string.IsNullOrWhiteSpace(Ward) &&
        !string.IsNullOrWhiteSpace(AddressLine);
}
