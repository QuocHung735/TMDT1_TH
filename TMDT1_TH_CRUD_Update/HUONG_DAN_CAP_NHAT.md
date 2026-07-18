# Bản cập nhật CRUD EF Core

Giải nén vào thư mục chứa solution và chọn ghi đè.

Đã triển khai:
- CRUD thật cho danh mục.
- Danh mục cha/con, slug tự động, lọc, ẩn/hiện, xóa mềm.
- Chặn xóa danh mục đang có con hoặc sản phẩm.
- CRUD thật cho thương hiệu.
- Chặn xóa thương hiệu đang có sản phẩm.
- Dashboard lấy số liệu thật từ Entity Framework Core.

Không cần Add-Migration vì không thay đổi model/database schema.
Chỉ cần Build và chạy ứng dụng.
