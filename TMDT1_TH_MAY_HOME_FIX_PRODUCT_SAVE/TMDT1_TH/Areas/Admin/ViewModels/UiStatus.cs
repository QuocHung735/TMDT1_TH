namespace TMDT1_TH.Areas.Admin.ViewModels;

public static class UiStatus
{
    public static string Class(string status) => status switch
    {
        "Đang bán" or "Đang hiển thị" or "Đang hoạt động" or "Đang áp dụng" => "status-active",
        "Sắp áp dụng" => "status-scheduled",
        "Sắp hết" or "Sắp hết hạn" => "status-warning",
        "Hết hàng" => "status-danger",
        "Bản nháp" or "Đã ẩn" or "Tạm ẩn" or "Tạm tắt" or "Hết hạn" => "status-muted",
        _ => "status-muted"
    };
}
