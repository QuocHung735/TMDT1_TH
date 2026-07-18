# Hướng dẫn cài bản sửa schema

1. Sao lưu project và database.
2. Đóng Visual Studio.
3. Giải nén ZIP tại thư mục chứa `TMDT1_TH.sln`.
4. Chọn **Replace/Ghi đè**.
5. Xóa `TMDT1_TH/bin` và `TMDT1_TH/obj`.
6. Mở solution và chọn **Build → Rebuild Solution**.
7. Chạy ứng dụng.

Ứng dụng sẽ tự bổ sung các cột/bảng marketplace còn thiếu trước khi mở module Sản phẩm hoặc Quản lý giá.

Nếu tài khoản SQL Server không có quyền ALTER/CREATE TABLE, chạy thủ công file:

```text
TMDT1_TH/Data/Database/Scripts/RepairMarketplaceProductSchema.sql
```

Commit đề xuất:

```text
fix(product-schema): auto-repair marketplace product and pricing database schema
```
