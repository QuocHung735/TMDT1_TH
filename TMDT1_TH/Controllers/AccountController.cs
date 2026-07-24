using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TMDT1_TH.Data;
using TMDT1_TH.Domain.Enums;
using TMDT1_TH.Domain.Identity;
using TMDT1_TH.ViewModels.Account;

namespace TMDT1_TH.Controllers;

[Route("tai-khoan")]
public sealed class AccountController(
    UserManager<ApplicationUser> userManager,
    SignInManager<ApplicationUser> signInManager,
    ApplicationDbContext db) : Controller
{
    private static readonly string[] AllowedGenders =
    {
        "Nam",
        "Nữ",
        "Khác"
    };

    private readonly UserManager<ApplicationUser> _userManager =
        userManager;
    private readonly SignInManager<ApplicationUser> _signInManager =
        signInManager;
    private readonly ApplicationDbContext _db = db;

    [AllowAnonymous]
    [HttpGet("dang-ky")]
    public IActionResult Register(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true)
            return RedirectToAction(nameof(Profile));

        return View(new RegisterViewModel
        {
            ReturnUrl = LocalReturnUrl(returnUrl)
        });
    }

    [AllowAnonymous]
    [HttpPost("dang-ky")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(
        RegisterViewModel model)
    {
        Normalize(model);

        ModelState.Clear();
        TryValidateModel(model);

        if (!ModelState.IsValid)
            return View(model);

        var existing =
            await _userManager.FindByEmailAsync(model.Email);

        if (existing is not null)
        {
            ModelState.AddModelError(
                nameof(model.Email),
                "Email này đã được sử dụng.");
            return View(model);
        }

        var user = new ApplicationUser
        {
            UserName = model.Email,
            Email = model.Email,
            EmailConfirmed = true,
            PhoneNumber = model.PhoneNumber,
            FullName = model.FullName,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        var createResult =
            await _userManager.CreateAsync(
                user,
                model.Password);

        if (!createResult.Succeeded)
        {
            AddIdentityErrors(createResult);
            return View(model);
        }

        var roleResult =
            await _userManager.AddToRoleAsync(
                user,
                "Customer");

        if (!roleResult.Succeeded)
        {
            await _userManager.DeleteAsync(user);
            AddIdentityErrors(roleResult);
            return View(model);
        }

        await _signInManager.SignInAsync(
            user,
            isPersistent: false);

        TempData["AccountMessage"] =
            "Đăng ký tài khoản thành công.";

        return RedirectToLocalOrDefault(
            model.ReturnUrl,
            "Account",
            nameof(Profile));
    }

    [AllowAnonymous]
    [HttpGet("dang-nhap")]
    public IActionResult Login(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true)
            return RedirectToAction(nameof(Profile));

        return View(new LoginViewModel
        {
            ReturnUrl = LocalReturnUrl(returnUrl)
        });
    }

    [AllowAnonymous]
    [HttpPost("dang-nhap")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        model.Email =
            model.Email?.Trim().ToLowerInvariant()
            ?? string.Empty;
        model.ReturnUrl = LocalReturnUrl(model.ReturnUrl);

        ModelState.Clear();
        TryValidateModel(model);

        if (!ModelState.IsValid)
            return View(model);

        var user =
            await _userManager.FindByEmailAsync(model.Email);

        if (user is null || !user.IsActive)
        {
            ModelState.AddModelError(
                string.Empty,
                "Email hoặc mật khẩu không chính xác.");
            return View(model);
        }

        var result =
            await _signInManager.PasswordSignInAsync(
                user,
                model.Password,
                model.RememberMe,
                lockoutOnFailure: true);

        if (result.Succeeded)
        {
            if (!string.IsNullOrWhiteSpace(model.ReturnUrl))
                return LocalRedirect(model.ReturnUrl);

            if (await _userManager.IsInRoleAsync(user, "Admin"))
            {
                return RedirectToAction(
                    "Index",
                    "Dashboard",
                    new { area = "Admin" });
            }

            return RedirectToAction(nameof(Profile));
        }

        ModelState.AddModelError(
            string.Empty,
            result.IsLockedOut
                ? "Tài khoản tạm khóa 15 phút do đăng nhập sai nhiều lần."
                : "Email hoặc mật khẩu không chính xác.");

        return View(model);
    }

    [Authorize]
    [HttpGet("")]
    public async Task<IActionResult> Profile()
    {
        var user =
            await _userManager.GetUserAsync(User);

        if (user is null)
            return Challenge();

        return View(await BuildProfileModelAsync(user));
    }

    [Authorize]
    [HttpPost("")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Profile(
        ProfileViewModel model)
    {
        var user =
            await _userManager.GetUserAsync(User);

        if (user is null)
            return Challenge();

        Normalize(model);

        // Chạy lại validation sau khi đã Trim để chuỗi chỉ gồm
        // khoảng trắng không thể vượt qua kiểm tra Required.
        ModelState.Clear();
        TryValidateModel(model);

        ModelState.Remove(nameof(model.Email));
        ModelState.Remove(nameof(model.CreatedAt));
        ModelState.Remove(nameof(model.Initials));
        ModelState.Remove(nameof(model.RoleName));
        ModelState.Remove(nameof(model.TotalOrders));
        ModelState.Remove(nameof(model.ActiveOrders));
        ModelState.Remove(nameof(model.CompletedOrders));
        ModelState.Remove(nameof(model.TotalReviews));

        if (model.DateOfBirth.HasValue)
        {
            var date = model.DateOfBirth.Value.Date;
            var today = DateTime.Today;

            if (date > today)
            {
                ModelState.AddModelError(
                    nameof(model.DateOfBirth),
                    "Ngày sinh không thể lớn hơn ngày hiện tại.");
            }
            else if (date < today.AddYears(-120))
            {
                ModelState.AddModelError(
                    nameof(model.DateOfBirth),
                    "Ngày sinh chưa hợp lệ.");
            }
        }

        if (!string.IsNullOrWhiteSpace(model.Gender) &&
            !AllowedGenders.Contains(model.Gender))
        {
            ModelState.AddModelError(
                nameof(model.Gender),
                "Giới tính chưa hợp lệ.");
        }

        if (!ModelState.IsValid)
        {
            await PopulateProfileSummaryAsync(
                model,
                user);

            return View(model);
        }

        user.FullName = model.FullName;
        user.PhoneNumber = model.PhoneNumber;
        user.DateOfBirth = model.DateOfBirth?.Date;
        user.Gender = NullIfWhiteSpace(model.Gender);
        user.Province = NullIfWhiteSpace(model.Province);
        user.District = NullIfWhiteSpace(model.District);
        user.Ward = NullIfWhiteSpace(model.Ward);
        user.AddressLine = NullIfWhiteSpace(model.AddressLine);

        var result =
            await _userManager.UpdateAsync(user);

        if (!result.Succeeded)
        {
            AddIdentityErrors(result);

            await PopulateProfileSummaryAsync(
                model,
                user);

            return View(model);
        }

        await _signInManager.RefreshSignInAsync(user);

        TempData["AccountMessage"] =
            "Đã cập nhật hồ sơ khách hàng.";

        return RedirectToAction(nameof(Profile));
    }

    [Authorize]
    [HttpGet("doi-mat-khau")]
    public IActionResult ChangePassword()
    {
        return View(new ChangePasswordViewModel());
    }

    [Authorize]
    [HttpPost("doi-mat-khau")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangePassword(
        ChangePasswordViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        var user = await _userManager.GetUserAsync(User);
        if (user is null)
            return Challenge();

        var result = await _userManager.ChangePasswordAsync(
            user,
            model.CurrentPassword,
            model.NewPassword);

        if (!result.Succeeded)
        {
            AddIdentityErrors(result);
            return View(model);
        }

        await _signInManager.RefreshSignInAsync(user);
        TempData["AccountMessage"] =
            "Đã thay đổi mật khẩu thành công.";

        return RedirectToAction(nameof(Profile));
    }

    [Authorize]
    [HttpPost("dang-xuat")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await _signInManager.SignOutAsync();
        return RedirectToAction("Index", "Home");
    }

    [AllowAnonymous]
    [HttpGet("tu-choi")]
    public IActionResult AccessDenied()
    {
        Response.StatusCode = StatusCodes.Status403Forbidden;
        return View();
    }

    private async Task<ProfileViewModel> BuildProfileModelAsync(
        ApplicationUser user)
    {
        var model = new ProfileViewModel
        {
            FullName = user.FullName,
            Email = user.Email ?? string.Empty,
            PhoneNumber = user.PhoneNumber ?? string.Empty,
            DateOfBirth = user.DateOfBirth,
            Gender = user.Gender,
            Province = user.Province,
            District = user.District,
            Ward = user.Ward,
            AddressLine = user.AddressLine
        };

        await PopulateProfileSummaryAsync(
            model,
            user);

        return model;
    }

    private async Task PopulateProfileSummaryAsync(
        ProfileViewModel model,
        ApplicationUser user)
    {
        var orderSummary = await _db.Orders
            .AsNoTracking()
            .Where(x => x.CustomerUserId == user.Id)
            .GroupBy(_ => 1)
            .Select(group => new
            {
                Total = group.Count(),
                Active = group.Count(x =>
                    x.Status != OrderStatus.Completed &&
                    x.Status != OrderStatus.Cancelled),
                Completed = group.Count(x =>
                    x.Status == OrderStatus.Completed)
            })
            .FirstOrDefaultAsync();

        model.Email = user.Email ?? string.Empty;
        model.CreatedAt = user.CreatedAt;
        model.Initials = BuildInitials(user.FullName);
        model.RoleName = User.IsInRole("Admin")
            ? "Quản trị viên"
            : "Khách hàng";
        model.TotalOrders = orderSummary?.Total ?? 0;
        model.ActiveOrders = orderSummary?.Active ?? 0;
        model.CompletedOrders = orderSummary?.Completed ?? 0;
        model.TotalReviews = await _db.ProductReviews
            .AsNoTracking()
            .CountAsync(x =>
                x.CustomerUserId == user.Id);
    }

    private IActionResult RedirectToLocalOrDefault(
        string? returnUrl,
        string controller,
        string action)
    {
        return !string.IsNullOrWhiteSpace(returnUrl) &&
               Url.IsLocalUrl(returnUrl)
            ? LocalRedirect(returnUrl)
            : RedirectToAction(action, controller)!;
    }

    private string? LocalReturnUrl(string? value)
    {
        return !string.IsNullOrWhiteSpace(value) &&
               Url.IsLocalUrl(value)
            ? value
            : null;
    }

    private void AddIdentityErrors(IdentityResult result)
    {
        foreach (var error in result.Errors)
        {
            ModelState.AddModelError(
                string.Empty,
                TranslateIdentityError(error));
        }
    }

    private static string TranslateIdentityError(
        IdentityError error)
    {
        return error.Code switch
        {
            "PasswordTooShort" =>
                "Mật khẩu chưa đủ 8 ký tự.",
            "PasswordRequiresNonAlphanumeric" =>
                "Mật khẩu cần ít nhất một ký tự đặc biệt.",
            "PasswordRequiresDigit" =>
                "Mật khẩu cần ít nhất một chữ số.",
            "PasswordRequiresLower" =>
                "Mật khẩu cần ít nhất một chữ thường.",
            "PasswordRequiresUpper" =>
                "Mật khẩu cần ít nhất một chữ hoa.",
            "PasswordMismatch" =>
                "Mật khẩu hiện tại không chính xác.",
            "DuplicateEmail" or "DuplicateUserName" =>
                "Email này đã được sử dụng.",
            _ => error.Description
        };
    }

    private static void Normalize(RegisterViewModel model)
    {
        model.FullName =
            model.FullName?.Trim() ?? string.Empty;
        model.Email =
            model.Email?.Trim().ToLowerInvariant()
            ?? string.Empty;
        model.PhoneNumber =
            model.PhoneNumber?.Trim() ?? string.Empty;
    }

    private static void Normalize(ProfileViewModel model)
    {
        model.FullName =
            model.FullName?.Trim() ?? string.Empty;
        model.PhoneNumber =
            model.PhoneNumber?.Trim() ?? string.Empty;
        model.Gender = NullIfWhiteSpace(model.Gender);
        model.Province = NullIfWhiteSpace(model.Province);
        model.District = NullIfWhiteSpace(model.District);
        model.Ward = NullIfWhiteSpace(model.Ward);
        model.AddressLine =
            NullIfWhiteSpace(model.AddressLine);
    }

    private static string BuildInitials(string fullName)
    {
        var parts = fullName
            .Split(
                ' ',
                StringSplitOptions.RemoveEmptyEntries);

        if (parts.Length == 0)
            return "KH";

        if (parts.Length == 1)
        {
            return parts[0]
                .Substring(
                    0,
                    Math.Min(2, parts[0].Length))
                .ToUpperInvariant();
        }

        return string.Concat(
            char.ToUpperInvariant(parts[0][0]),
            char.ToUpperInvariant(parts[^1][0]));
    }

    private static string? NullIfWhiteSpace(
        string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim();
    }
}
