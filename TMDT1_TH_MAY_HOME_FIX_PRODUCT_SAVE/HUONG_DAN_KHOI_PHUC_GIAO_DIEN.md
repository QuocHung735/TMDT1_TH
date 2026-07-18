# Hướng dẫn khôi phục giao diện bước trước

## Phạm vi

Bản cập nhật này chỉ khôi phục giao diện pastel của bước trước. Toàn bộ chức năng dữ liệu thật của giai đoạn 5 vẫn được giữ nguyên, bao gồm CRUD thị trường và các truy vấn SQL Server.

## Cài đặt

1. Đóng Visual Studio.
2. Giải nén ZIP tại thư mục chứa `TMDT1_TH.sln`.
3. Chọn Replace/Ghi đè.
4. Xóa `TMDT1_TH/bin` và `TMDT1_TH/obj`.
5. Mở solution, Clean Solution rồi Rebuild Solution.

Không cần Add-Migration hoặc Update-Database.

## Giao diện

- CSS chính: `TMDT1_TH/wwwroot/admin/css/admin.css`.
- Font: Be Vietnam Pro.
- Biểu tượng: Bootstrap Icons qua CDN.
- Không sử dụng Bootstrap CSS framework.
