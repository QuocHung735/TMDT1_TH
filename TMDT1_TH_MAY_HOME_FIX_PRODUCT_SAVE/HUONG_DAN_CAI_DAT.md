# Mây Home – Hướng dẫn cập nhật giai đoạn 5

## Cài đặt

1. Đóng Visual Studio.
2. Giải nén ZIP tại thư mục chứa `TMDT1_TH.sln`.
3. Chọn **Replace/Ghi đè**.
4. Xóa `TMDT1_TH/bin` và `TMDT1_TH/obj` nếu Visual Studio còn cache.
5. Mở solution và chọn **Build → Rebuild Solution**.
6. Chạy project và truy cập `/Admin/Markets`.

## Database

Nếu database đã được tạo ở các giai đoạn trước, không chạy migration mới.

Nếu chưa có database:

```powershell
Add-Migration InitialCommerceSchema
Update-Database
```

Sau khi chạy, ứng dụng tự cài trigger kiểm tra chồng lịch giá và ghi lịch sử giá.

## Dữ liệu thật

Các trang sau lấy dữ liệu trực tiếp từ SQL Server:

```text
/Admin/Dashboard
/Admin/Categories
/Admin/Brands
/Admin/Products
/Admin/Pricing
/Admin/Markets
```

Ứng dụng không tự thêm catalog đồ gia dụng mẫu khi khởi động. Danh mục, thương hiệu và sản phẩm bạn tạo sẽ được sử dụng trực tiếp ở các form liên quan.

## CSS riêng

Không sử dụng Bootstrap hoặc Bootstrap Icons. Toàn bộ giao diện nằm trong:

```text
TMDT1_TH/wwwroot/admin/css/style.css
TMDT1_TH/wwwroot/admin/css/icons.css
```

Không cần kết nối CDN để giao diện hoạt động.
