using Microsoft.AspNetCore.Mvc;
using TMDT1_TH.Areas.Admin.ViewModels;

namespace TMDT1_TH.Areas.Admin.Controllers;

[Area("Admin")]
public class MarketsController : Controller
{
    public IActionResult Index() => View(new MarketsViewModel
    {
        Items =
        [
            new(1, "ONLINE", "Kênh trực tuyến", "VND", 328, "Đang hoạt động", true),
            new(2, "VN-HCM", "Thành phố Hồ Chí Minh", "VND", 286, "Đang hoạt động", false),
            new(3, "VN-HN", "Hà Nội", "VND", 240, "Đang hoạt động", false),
            new(4, "VN-DN", "Đà Nẵng", "VND", 86, "Tạm ẩn", false)
        ]
    });
}
