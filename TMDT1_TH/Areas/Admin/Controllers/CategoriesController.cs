using Microsoft.AspNetCore.Mvc;
using TMDT1_TH.Areas.Admin.ViewModels;

namespace TMDT1_TH.Areas.Admin.Controllers;

[Area("Admin")]
public class CategoriesController : Controller
{
    public IActionResult Index() => View(new CategoriesViewModel
    {
        Items =
        [
            new(1, "Thời trang nữ", "thoi-trang-nu", "—", 324, "Đang hiển thị", 0, "bi-gender-female"),
            new(2, "Áo nữ", "ao-nu", "Thời trang nữ", 128, "Đang hiển thị", 1, "bi-person-standing-dress"),
            new(3, "Váy & đầm", "vay-dam", "Thời trang nữ", 96, "Đang hiển thị", 1, "bi-stars"),
            new(4, "Quần nữ", "quan-nu", "Thời trang nữ", 100, "Đang hiển thị", 1, "bi-person"),
            new(5, "Phụ kiện", "phu-kien", "—", 87, "Đang hiển thị", 0, "bi-handbag"),
            new(6, "Bộ sưu tập cũ", "bo-suu-tap-cu", "—", 0, "Đã ẩn", 0, "bi-archive")
        ]
    });

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult CreateDemo(string? name)
    {
        TempData["Success"] = string.IsNullOrWhiteSpace(name)
            ? "Đã mở luồng tạo danh mục mẫu."
            : $"Đã tạo danh mục mẫu “{name}”.";
        return RedirectToAction(nameof(Index));
    }
}
