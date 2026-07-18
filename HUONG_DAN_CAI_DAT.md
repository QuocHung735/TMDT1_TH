# Hướng dẫn cài đặt

1. Sao lưu project và database hiện tại.
2. Đóng Visual Studio.
3. Giải nén ZIP tại thư mục chứa `TMDT1_TH.sln`.
4. Chọn Replace/Ghi đè file trùng.
5. Xóa thư mục `TMDT1_TH/bin` và `TMDT1_TH/obj`.
6. Mở lại solution và Rebuild Solution.
7. Kiểm tra chuỗi kết nối trong `appsettings.json`.
8. Chạy migration:

```powershell
Add-Migration UpgradeMarketplaceProductModule
Update-Database
```

9. Chạy ứng dụng và truy cập `/Admin/Products`.

## Lưu ý

- Đây là bản thay đổi schema, bắt buộc cập nhật database.
- Không cần xóa dữ liệu sản phẩm cũ.
- Các cột mới có giá trị mặc định an toàn cho bản ghi cũ.
- Ứng dụng không tự seed sản phẩm mẫu.
