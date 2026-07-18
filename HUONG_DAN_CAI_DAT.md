# TMDT1_TH – Giai đoạn 2: CRUD danh mục, thương hiệu và dashboard database

## Cài đặt

1. Đóng Visual Studio.
2. Giải nén file ZIP tại thư mục đang chứa `TMDT1_TH.sln`.
3. Chọn **Replace / Ghi đè** khi Windows hỏi.
4. Mở lại solution và kiểm tra `ConnectionStrings:DefaultConnection` trong `TMDT1_TH/appsettings.json`.
5. Build lại solution và chạy project.

## Migration

### Trường hợp chưa tạo database

Mở **Package Manager Console**, chọn Default project là `TMDT1_TH`, sau đó chạy:

```powershell
Add-Migration InitialCommerceSchema
Update-Database
```

### Trường hợp đã chạy migration của gói Models trước đó

Giai đoạn này không thay đổi cấu trúc bảng nên **không cần tạo migration mới**. Chỉ cần giải nén, build và chạy ứng dụng.

Nếu Visual Studio vẫn giữ cache lỗi cũ:

1. Đóng Visual Studio.
2. Xóa thư mục `TMDT1_TH/bin` và `TMDT1_TH/obj`.
3. Mở lại solution và Rebuild.

## Chức năng đã nối database

### Danh mục

- Hiển thị danh mục nhiều cấp.
- Tìm kiếm theo tên hoặc slug.
- Lọc theo trạng thái.
- Thêm và chỉnh sửa trong modal.
- Tự sinh slug tiếng Việt và tự thêm hậu tố khi trùng.
- Chọn danh mục cha.
- Chặn chọn chính nó hoặc danh mục con làm cha.
- Ẩn/hiện danh mục.
- Xóa mềm.
- Chặn xóa khi còn danh mục con hoặc sản phẩm.
- Thống kê số danh mục và số sản phẩm đã gán.

### Thương hiệu

- Tìm kiếm theo tên, slug và quốc gia.
- Lọc trạng thái.
- Thêm và chỉnh sửa trong modal.
- Tự sinh slug và chống trùng.
- Quản lý quốc gia, website, logo URL và mô tả.
- Kích hoạt/tạm ẩn.
- Xóa mềm.
- Chặn xóa thương hiệu đang có sản phẩm.

### Dashboard

Dashboard đọc trực tiếp từ SQL Server:

- Tổng sản phẩm.
- Sản phẩm đang bán.
- Biến thể đang hoạt động.
- Sản phẩm sắp hết và hết hàng.
- Lịch giá sẽ áp dụng trong 7 ngày.
- Danh sách sản phẩm gần đây.
- Giá bán hiện tại.
- Hoạt động tạo/cập nhật gần đây.
- Biểu đồ sức khỏe tồn kho.

## Đường dẫn kiểm tra

```text
/Admin/Dashboard
/Admin/Categories
/Admin/Brands
```

## Lưu ý

- Trang sản phẩm, biến thể và giá vẫn giữ giao diện mô phỏng của giai đoạn trước.
- Giai đoạn tiếp theo là nối CRUD sản phẩm, hình ảnh và sinh tổ hợp biến thể vào database.
- Xóa danh mục và thương hiệu sử dụng soft delete, không xóa vật lý dữ liệu.
