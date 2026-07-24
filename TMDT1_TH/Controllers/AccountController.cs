using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using TMDT1_TH.Domain.Identity;
using TMDT1_TH.ViewModels.Account;

namespace TMDT1_TH.Controllers;

[Route("tai-khoan")]
public sealed class AccountController(
    UserManager<ApplicationUser> userManager,
    SignInManager<ApplicationUser> signInManager) : Controller
{
    private readonly UserManager<ApplicationUser> _userManager =
        userManager;
    private readonly SignInManager<ApplicationUser> _signInManager =
        signInManager;

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
            "MyOrders",
            "Index");
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
        model.Email = model.Email.Trim().ToLowerInvariant();
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

            return RedirectToAction("Index", "MyOrders");
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

        return View(new ProfileViewModel
        {
            FullName = user.FullName,
            Email = user.Email ?? string.Empty,
            PhoneNumber = user.PhoneNumber ?? string.Empty
        });
    }

    [Authorize]
    [HttpPost("")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Profile(
        ProfileViewModel model)
    {
        model.FullName = model.FullName.Trim();
        model.PhoneNumber = model.PhoneNumber.Trim();

        ModelState.Remove(nameof(model.Email));

        if (!ModelState.IsValid)
            return View(model);

        var user =
            await _userManager.GetUserAsync(User);

        if (user is null)
            return Challenge();

        user.FullName = model.FullName;
        user.PhoneNumber = model.PhoneNumber;

        var result =
            await _userManager.UpdateAsync(user);

        if (!result.Succeeded)
        {
            AddIdentityErrors(result);
            model.Email = user.Email ?? string.Empty;
            return View(model);
        }

        await _signInManager.RefreshSignInAsync(user);

        TempData["AccountMessage"] =
            "Đã cập nhật thông tin tài khoản.";

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
            "DuplicateEmail" or "DuplicateUserName" =>
                "Email này đã được sử dụng.",
            _ => error.Description
        };
    }

    private static void Normalize(RegisterViewModel model)
    {
        model.FullName = model.FullName.Trim();
        model.Email = model.Email.Trim().ToLowerInvariant();
        model.PhoneNumber = model.PhoneNumber.Trim();
    }
}
