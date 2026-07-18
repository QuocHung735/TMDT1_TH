# Sửa lỗi auto-upgrade schema V2

## Nguyên nhân

Phiên bản trước gửi toàn bộ DDL trong một SQL batch. SQL Server biên dịch cả batch trước khi thực thi, nên các CHECK CONSTRAINT ở cuối batch vẫn không nhận ra những cột vừa được ALTER TABLE ADD ở đầu batch và báo `Invalid column name`.

## Cách sửa

- Mỗi cột được thêm bằng một batch riêng.
- Tạo `ProductSpecifications` ở batch riêng.
- Chuẩn hóa dữ liệu cũ trước khi thêm CHECK CONSTRAINT.
- Index và từng constraint được tạo ở các batch riêng.
- Tất cả batch vẫn nằm trong cùng transaction của EF Core.
- Khi SQL lỗi, thông báo mới hiển thị số lỗi, dòng và nội dung lỗi gốc.
- Script SSMS dùng `GO` để tách batch đúng cách.

## Sau khi cập nhật

Xóa `bin`, `obj`, Rebuild Solution rồi chạy lại ứng dụng. Installer có tính idempotent nên có thể chạy lại an toàn sau lần nâng cấp dở dang.
