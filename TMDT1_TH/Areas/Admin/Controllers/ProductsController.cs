using Microsoft.AspNetCore.Mvc;
using TMDT1_TH.Areas.Admin.ViewModels;

namespace TMDT1_TH.Areas.Admin.Controllers;

[Area("Admin")]
public class ProductsController : Controller
{
    public IActionResult Index() => View(new ProductsViewModel
    {
        Items =
        [
            new(1, "Áo sơ mi Linen Breeze", "SHIRT-LB", "Áo nữ", "Lumi Basic", 9, "489.000đ", 86, "Đang bán", "LB", "purple"),
            new(2, "Quần ống rộng Cloudy", "PANT-CL", "Quần nữ", "Mây Studio", 6, "529.000đ", 42, "Đang bán", "CL", "mint"),
            new(3, "Túi mini Pastel Day", "BAG-PD", "Phụ kiện", "Nắng Atelier", 4, "349.000đ", 12, "Sắp hết", "PD", "amber"),
            new(4, "Váy midi Soft Bloom", "DRESS-SB", "Váy & đầm", "Cloud Nine", 8, "699.000đ", 0, "Hết hàng", "SB", "rose"),
            new(5, "Cardigan Morning Mist", "CARD-MM", "Áo nữ", "Mây Studio", 5, "579.000đ", 28, "Bản nháp", "MM", "blue")
        ]
    });

    public IActionResult Create() => View("Editor", new ProductEditorViewModel());

    public IActionResult Edit(int id) => View("Editor", new ProductEditorViewModel
    {
        Id = id,
        Name = "Áo sơ mi Linen Breeze",
        Sku = "SHIRT-LB",
        Category = "Áo nữ",
        Brand = "Lumi Basic",
        Description = "Thiết kế linen nhẹ, phom thoải mái và dễ phối trong nhiều hoàn cảnh."
    });

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult SaveDemo(string? name)
    {
        TempData["Success"] = $"Đã lưu bản nháp sản phẩm “{(string.IsNullOrWhiteSpace(name) ? "Sản phẩm mới" : name)}”.";
        return RedirectToAction(nameof(Index));
    }
}
