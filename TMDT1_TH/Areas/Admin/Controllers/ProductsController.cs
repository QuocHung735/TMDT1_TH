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
            new(1, "Nồi chiên không dầu AirCare", "HOME-AF-6L", "Điện gia dụng", "Mây Home", 4, "1.890.000đ", 36, "Đang bán", "AC", "purple"),
            new(2, "Chảo chống dính Ceramic Glow", "HOME-PAN-CG", "Nồi & chảo", "Bếp Xinh", 6, "429.000đ", 72, "Đang bán", "CG", "mint"),
            new(3, "Bộ hộp bảo quản FreshBox", "HOME-BOX-FB", "Lưu trữ thực phẩm", "PureNest", 3, "289.000đ", 9, "Sắp hết", "FB", "amber"),
            new(4, "Cây lau nhà xoay 360 CleanSpin", "HOME-MOP-CS", "Vệ sinh nhà cửa", "CleanJoy", 2, "359.000đ", 0, "Hết hàng", "CS", "rose"),
            new(5, "Kệ đa năng FlexiRack 4 tầng", "HOME-RACK-F4", "Lưu trữ & sắp xếp", "Mây Home", 3, "549.000đ", 24, "Bản nháp", "FR", "blue")
        ]
    });

    public IActionResult Create() => View("Editor", new ProductEditorViewModel());

    public IActionResult Edit(int id) => View("Editor", new ProductEditorViewModel
    {
        Id = id,
        Name = "Nồi chiên không dầu AirCare 6L",
        Sku = "HOME-AF-6L",
        Category = "Điện gia dụng",
        Brand = "Mây Home",
        Description = "Dung tích 6 lít, bảng điều khiển cảm ứng và lòng nồi chống dính dễ vệ sinh."
    });

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult SaveDemo(string? name)
    {
        TempData["Success"] = $"Đã lưu bản nháp sản phẩm “{(string.IsNullOrWhiteSpace(name) ? "Sản phẩm mới" : name)}”.";
        return RedirectToAction(nameof(Index));
    }
}
