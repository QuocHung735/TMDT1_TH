using Microsoft.AspNetCore.Identity;
using TMDT1_TH.Domain.Identity;

namespace TMDT1_TH.Data.Identity;

public static class IdentitySeeder
{
    private static readonly string[] Roles =
    {
        "Admin",
        "Customer"
    };

    public static async Task SeedAsync(
        IServiceProvider services,
        IConfiguration configuration,
        ILogger logger)
    {
        await using var scope = services.CreateAsyncScope();

        var roleManager =
            scope.ServiceProvider
                .GetRequiredService<RoleManager<IdentityRole<int>>>();

        var userManager =
            scope.ServiceProvider
                .GetRequiredService<UserManager<ApplicationUser>>();

        foreach (var roleName in Roles)
        {
            if (await roleManager.RoleExistsAsync(roleName))
                continue;

            EnsureSucceeded(
                await roleManager.CreateAsync(
                    new IdentityRole<int>(roleName)),
                $"Không thể tạo vai trò {roleName}");
        }

        var section =
            configuration.GetSection("BootstrapAdmin");

        if (!section.GetValue<bool>("Enabled"))
            return;

        var email = section["Email"]?.Trim();
        var password = section["Password"];
        var fullName =
            section["FullName"]?.Trim()
            ?? "Quản trị viên Mây Home";

        if (string.IsNullOrWhiteSpace(email) ||
            string.IsNullOrWhiteSpace(password))
        {
            logger.LogWarning(
                "BootstrapAdmin đã bật nhưng thiếu Email hoặc Password.");
            return;
        }

        var admin =
            await userManager.FindByEmailAsync(email);

        if (admin is null)
        {
            admin = new ApplicationUser
            {
                UserName = email,
                Email = email,
                EmailConfirmed = true,
                FullName = fullName,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            EnsureSucceeded(
                await userManager.CreateAsync(admin, password),
                "Không thể tạo tài khoản quản trị mặc định");
        }

        if (!await userManager.IsInRoleAsync(admin, "Admin"))
        {
            EnsureSucceeded(
                await userManager.AddToRoleAsync(admin, "Admin"),
                "Không thể gán vai trò Admin");
        }

        logger.LogInformation(
            "Đã bảo đảm tài khoản quản trị {AdminEmail}.",
            email);
    }

    private static void EnsureSucceeded(
        IdentityResult result,
        string message)
    {
        if (result.Succeeded)
            return;

        var errors = string.Join(
            "; ",
            result.Errors.Select(x => x.Description));

        throw new InvalidOperationException(
            $"{message}: {errors}");
    }
}
