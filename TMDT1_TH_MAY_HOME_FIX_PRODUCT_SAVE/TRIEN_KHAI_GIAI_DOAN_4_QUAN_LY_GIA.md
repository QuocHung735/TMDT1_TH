# Giai đoạn 4 – Quản lý giá bằng database

## Chức năng đã triển khai

- Danh sách lịch giá đọc trực tiếp từ `PriceSchedules`.
- Tìm theo sản phẩm, SKU, biến thể hoặc thị trường.
- Lọc theo thị trường và trạng thái.
- Thiết lập giá cho toàn sản phẩm hoặc riêng từng biến thể.
- Lưu đủ giá vốn, giá niêm yết và giá bán.
- Giá theo khoảng thời gian hoặc vô hạn.
- Tạo, chỉnh sửa, bật/tắt và xóa lịch giá.
- Kiểm tra khoảng thời gian chồng lấn tại controller và trigger SQL Server.
- Thống kê giá đang áp dụng, sắp áp dụng và sắp hết hạn.
- Hiển thị lịch sử giá do trigger ghi tự động.
- Xuất lịch sử giá thành CSV.

## Migration

Giai đoạn này không thay đổi Models hoặc cấu trúc bảng nên không cần tạo migration mới.

## Cài đặt

1. Giải nén tại thư mục chứa `TMDT1_TH.sln`.
2. Chọn ghi đè file trùng.
3. Xóa `bin` và `obj` nếu Visual Studio còn cache lỗi cũ.
4. Rebuild Solution và chạy `/Admin/Pricing`.
