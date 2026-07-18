using Microsoft.AspNetCore.Mvc;
using TMDT1_TH.Areas.Admin.ViewModels;

namespace TMDT1_TH.Areas.Admin.Controllers;

[Area("Admin")]
public class PricingController : Controller
{
    public IActionResult Index() => View(new PricingViewModel
    {
        Items =
        [
            new(1, "Áo sơ mi Linen Breeze", "Trắng / M", "Online", "245.000đ", "549.000đ", "489.000đ", "Không giới hạn", "Đang áp dụng"),
            new(2, "Áo sơ mi Linen Breeze", "Tím / L", "Hồ Chí Minh", "245.000đ", "549.000đ", "459.000đ", "20/07 – 31/07", "Sắp áp dụng"),
            new(3, "Quần ống rộng Cloudy", "Kem / S", "Online", "280.000đ", "599.000đ", "529.000đ", "Không giới hạn", "Đang áp dụng"),
            new(4, "Túi mini Pastel Day", "Xanh mint", "Hà Nội", "170.000đ", "399.000đ", "349.000đ", "01/07 – 22/07", "Sắp hết hạn")
        ],
        History =
        [
            new("Áo sơ mi Linen Breeze", "Trắng / M", "Online", "Giá bán", "479.000đ", "489.000đ", "+2,1%", "Quốc Hưng", "18/07/2026 07:22", "up"),
            new("Quần ống rộng Cloudy", "Kem / S", "Online", "Giá niêm yết", "629.000đ", "599.000đ", "-4,8%", "Quốc Hưng", "17/07/2026 14:05", "down"),
            new("Túi mini Pastel Day", "Xanh mint", "Hà Nội", "Giá bán", "329.000đ", "349.000đ", "+6,1%", "Price Manager", "16/07/2026 10:18", "up")
        ]
    });

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult SaveDemo(string? product)
    {
        TempData["Success"] = $"Đã lưu lịch giá mẫu cho “{(string.IsNullOrWhiteSpace(product) ? "sản phẩm" : product)}”.";
        return RedirectToAction(nameof(Index));
    }
}
