# Sửa lỗi không lưu được sản phẩm

## Nguyên nhân chính

`Program.cs` bật `EnableRetryOnFailure(3)` trong khi `ProductsController.Save` tự mở transaction bằng `BeginTransactionAsync()`.
EF Core không cho dùng transaction tự mở trực tiếp với `SqlServerRetryingExecutionStrategy`, vì vậy yêu cầu lưu dừng trước khi thêm dữ liệu.

## Đã sửa

- Bỏ `EnableRetryOnFailure(3)` cho kết nối SQL Server cục bộ.
- Chuyển `BeginTransactionAsync()` vào trong `try`.
- Khi lỗi, transaction tự rollback khi dispose và ChangeTracker được xóa.
- Form chỉ định rõ `Admin / Products / Save`.
- Hiển thị rõ lỗi transaction nếu còn phát sinh.

Không thay đổi model hoặc cấu trúc database, nên không cần migration mới.
