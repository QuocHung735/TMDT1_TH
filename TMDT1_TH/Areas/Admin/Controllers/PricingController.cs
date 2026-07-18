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
            new(1, "Nồi chiên không dầu AirCare", "6 lít / Kem", "Online", "1.180.000đ", "2.490.000đ", "1.890.000đ", "Không giới hạn", "Đang áp dụng"),
            new(2, "Nồi chiên không dầu AirCare", "4 lít / Xanh mint", "Hồ Chí Minh", "920.000đ", "1.990.000đ", "1.590.000đ", "20/07 – 31/07", "Sắp áp dụng"),
            new(3, "Chảo chống dính Ceramic Glow", "28 cm / Kem", "Online", "230.000đ", "549.000đ", "429.000đ", "Không giới hạn", "Đang áp dụng"),
            new(4, "Bộ hộp bảo quản FreshBox", "Bộ 5 hộp", "Hà Nội", "145.000đ", "349.000đ", "289.000đ", "01/07 – 22/07", "Sắp hết hạn")
        ],
        History =
        [
            new("Nồi chiên không dầu AirCare", "6 lít / Kem", "Online", "Giá bán", "1.790.000đ", "1.890.000đ", "+5,6%", "Quốc Hưng", "18/07/2026 07:22", "up"),
            new("Chảo chống dính Ceramic Glow", "28 cm / Kem", "Online", "Giá niêm yết", "599.000đ", "549.000đ", "-8,3%", "Quốc Hưng", "17/07/2026 14:05", "down"),
            new("Bộ hộp bảo quản FreshBox", "Bộ 5 hộp", "Hà Nội", "Giá bán", "269.000đ", "289.000đ", "+7,4%", "Price Manager", "16/07/2026 10:18", "up")
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
