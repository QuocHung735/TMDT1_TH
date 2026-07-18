using Microsoft.AspNetCore.Mvc;
using TMDT1_TH.Areas.Admin.ViewModels;

namespace TMDT1_TH.Areas.Admin.Controllers;

[Area("Admin")]
public class DashboardController : Controller
{
    public IActionResult Index()
    {
        var model = new DashboardViewModel
        {
            Metrics =
            [
                new("Tổng sản phẩm", "1.248", "+8,2%", "bi-box-seam", "purple", "so với tháng trước"),
                new("Biến thể đang bán", "3.684", "+12,4%", "bi-diagram-2", "mint", "126 biến thể mới"),
                new("Sắp hết hàng", "24", "-6,8%", "bi-exclamation-triangle", "amber", "cần bổ sung tồn kho"),
                new("Giá chờ áp dụng", "18", "+4", "bi-calendar2-check", "blue", "trong 7 ngày tới")
            ],
            RecentProducts =
            [
                new("Áo sơ mi Linen Breeze", "SHIRT-LB", "Áo sơ mi", "489.000đ", "86", "Đang bán", "LB", "purple"),
                new("Quần ống rộng Cloudy", "PANT-CL", "Quần nữ", "529.000đ", "42", "Đang bán", "CL", "mint"),
                new("Túi mini Pastel Day", "BAG-PD", "Phụ kiện", "349.000đ", "12", "Sắp hết", "PD", "amber"),
                new("Váy midi Soft Bloom", "DRESS-SB", "Váy", "699.000đ", "0", "Hết hàng", "SB", "rose")
            ],
            Activities =
            [
                new("bi-tags", "Điều chỉnh giá bán", "Áo sơ mi Linen Breeze · Thị trường HCM", "12 phút trước", "purple"),
                new("bi-box-seam", "Thêm sản phẩm mới", "Váy midi Soft Bloom · 8 biến thể", "35 phút trước", "mint"),
                new("bi-diagram-3", "Cập nhật danh mục", "Đổi thứ tự hiển thị danh mục Phụ kiện", "1 giờ trước", "blue"),
                new("bi-patch-check", "Thêm thương hiệu", "Thương hiệu nội địa Mây Studio", "2 giờ trước", "amber")
            ],
            PriceAlerts =
            [
                new("Áo sơ mi Linen Breeze", "Hồ Chí Minh", "Giá bán", "20/07/2026", "Sắp áp dụng"),
                new("Quần ống rộng Cloudy", "Online", "Giá niêm yết", "21/07/2026", "Sắp áp dụng"),
                new("Túi mini Pastel Day", "Hà Nội", "Giá bán", "22/07/2026", "Sắp áp dụng")
            ]
        };

        return View(model);
    }
}
