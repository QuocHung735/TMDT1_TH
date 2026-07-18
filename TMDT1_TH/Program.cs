using Microsoft.EntityFrameworkCore;
using TMDT1_TH.Data;
using TMDT1_TH.Data.Database;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Chưa cấu hình ConnectionStrings:DefaultConnection trong appsettings.json.");

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString, sqlOptions =>
    {
        // Không bật EnableRetryOnFailure tại đây vì luồng lưu sản phẩm
        // sử dụng transaction do ứng dụng chủ động tạo.
        sqlOptions.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName);
    }));

builder.Services.AddControllersWithViews();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}
else
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthorization();

app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=Dashboard}/{action=Index}/{id?}");

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapControllers();

// Tự bổ sung các cột/bảng marketplace còn thiếu trước khi controller truy vấn dữ liệu.
await MarketplaceProductSchemaInstaller.EnsureUpgradedAsync(app.Services, app.Logger);

// Cài/cập nhật trigger kiểm tra chồng lịch và ghi lịch sử giá.
await DatabaseTriggerInstaller.TryInstallAsync(app.Services, app.Logger);

app.Run();
