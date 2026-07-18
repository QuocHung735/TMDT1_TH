# TMDT1_TH – UI/UX Admin + Entity Framework Core Models

## 1. Cài bộ file

1. Đóng Visual Studio.
2. Giải nén ZIP tại thư mục đang chứa `TMDT1_TH.sln`.
3. Chọn **Replace/Ghi đè** file trùng.
4. Mở lại solution.
5. Kiểm tra chuỗi kết nối trong `TMDT1_TH/appsettings.json`.

Chuỗi mặc định:

```json
"DefaultConnection": "Server=.;Database=TMDT1_TH_DB;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=true"
```

Nếu SQL Server dùng instance khác, chỉ sửa phần `Server`, ví dụ:

```text
Server=.\\SQLEXPRESS
Server=localhost
Server=TENMAY\\NGUYEN1
```

## 2. Tạo database

Mở **Tools → NuGet Package Manager → Package Manager Console**.

Chọn `TMDT1_TH` tại ô **Default project**, sau đó chạy:

```powershell
Add-Migration InitialCommerceSchema
Update-Database
```

Sau đó nhấn F5 chạy ứng dụng. Ứng dụng sẽ tự tạo/cập nhật hai trigger:

- `TRG_PriceSchedules_Validate`: ngăn lịch giá chồng thời gian.
- `TRG_PriceSchedules_History`: tự ghi lịch sử thêm, sửa và xóa giá.

## 3. Các bảng được tạo

- `Categories`: danh mục nhiều cấp.
- `Brands`: thương hiệu.
- `Products`: sản phẩm.
- `ProductImages`: hình ảnh sản phẩm và biến thể.
- `ProductOptions`: thuộc tính như Màu sắc, Kích thước.
- `ProductOptionValues`: giá trị thuộc tính.
- `ProductVariants`: biến thể, SKU, barcode, tồn kho.
- `ProductVariantValues`: liên kết biến thể với giá trị thuộc tính.
- `Markets`: thị trường/kênh bán.
- `PriceSchedules`: ba giá và thời gian áp dụng.
- `PriceHistories`: lịch sử biến động giá.

## 4. Quy tắc giá

Mỗi bản ghi `PriceSchedule` có đủ:

- `CostPrice`: giá vốn.
- `ListPrice`: giá niêm yết.
- `SalePrice`: giá bán.

Một lịch giá chỉ được gắn với **sản phẩm hoặc biến thể**, không được gắn đồng thời cả hai. `ValidTo = null` nghĩa là áp dụng vô thời hạn.

## 5. Dữ liệu seed

Migration đầu tiên tự thêm ba thị trường:

- `ONLINE` – Kênh trực tuyến.
- `VN-HCM` – Thành phố Hồ Chí Minh.
- `VN-HN` – Hà Nội.

## 6. Lưu ý

Giao diện admin hiện vẫn sử dụng dữ liệu minh họa trong controller để bạn xem UI ngay. Models, DbContext, quan hệ và database đã sẵn sàng; bước tiếp theo là thay dữ liệu mẫu bằng truy vấn `ApplicationDbContext` và hoàn thiện CRUD.
