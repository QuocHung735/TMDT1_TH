using Microsoft.AspNetCore.Mvc;
using TMDT1_TH.Areas.Admin.ViewModels;

namespace TMDT1_TH.Areas.Admin.Controllers;

[Area("Admin")]
public class BrandsController : Controller
{
    public IActionResult Index() => View(new BrandsViewModel
    {
        Items =
        [
            new(1, "Mây Studio", "Việt Nam", 124, "Đang hoạt động", "MS", "purple"),
            new(2, "Lumi Basic", "Việt Nam", 96, "Đang hoạt động", "LB", "mint"),
            new(3, "Nắng Atelier", "Việt Nam", 58, "Đang hoạt động", "NA", "amber"),
            new(4, "Cloud Nine", "Hàn Quốc", 42, "Đang hoạt động", "C9", "blue"),
            new(5, "Old Season", "Việt Nam", 0, "Tạm ẩn", "OS", "rose")
        ]
    });

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult CreateDemo(string? name)
    {
        TempData["Success"] = $"Đã lưu thương hiệu mẫu “{(string.IsNullOrWhiteSpace(name) ? "Thương hiệu mới" : name)}”.";
        return RedirectToAction(nameof(Index));
    }
}
