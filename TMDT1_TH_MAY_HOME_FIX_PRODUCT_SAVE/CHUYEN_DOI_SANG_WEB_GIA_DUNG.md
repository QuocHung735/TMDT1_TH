# Mây Home – Chuyển website sang bán đồ gia dụng

## Cài đặt

1. Đóng Visual Studio.
2. Giải nén file ZIP tại thư mục chứa `TMDT1_TH.sln`.
3. Chọn **Replace/Ghi đè**.
4. Mở lại solution và chạy ứng dụng.

Không thay đổi Models hoặc cấu trúc bảng, vì vậy **không cần Add-Migration**.

Ứng dụng không tự thêm catalog mẫu khi khởi động. Dữ liệu hiển thị là dữ liệu đang có trong SQL Server hoặc do quản trị viên tạo mới.

## Nội dung đã đổi

- Thương hiệu giao diện: **Mây Home Commerce / Gia Dụng Mây**.
- Danh mục: Nhà bếp, Nồi & chảo, Lưu trữ thực phẩm, Điện gia dụng, Vệ sinh nhà cửa, Lưu trữ & sắp xếp.
- Hệ thống hỗ trợ thương hiệu đồ gia dụng do quản trị viên tự tạo.
- Hệ thống hỗ trợ sản phẩm nhà bếp, điện gia dụng, vệ sinh và lưu trữ do quản trị viên tự nhập.
- Biến thể chuyển sang dung tích, kích thước và màu sắc.
- Bảng giá vẫn giữ đủ giá vốn, giá niêm yết và giá bán theo thị trường/thời gian.

Nếu database hiện có dữ liệu thời trang, dữ liệu đó không bị tự động xóa để bảo vệ dữ liệu của bạn. Bạn có thể xóa mềm các sản phẩm cũ từ trang quản trị.
