# Sửa schema module Sản phẩm và Quản lý giá

## Lỗi đã xử lý

Database hiện tại được tạo từ model cũ nhưng code đã sử dụng model marketplace mới, dẫn đến các lỗi:

- `Invalid object name 'ProductSpecifications'`
- `Invalid column name 'LowStockThreshold'`
- `Invalid column name 'SortOrder'`
- `Invalid column name 'CountryOfOrigin'`
- `Invalid column name 'ManufacturerAddress'`
- `Invalid column name 'ManufacturerName'`
- `Invalid column name 'MaxPurchaseQuantity'`
- `Invalid column name 'MinPurchaseQuantity'`
- `Invalid column name 'ModelNumber'`
- `Invalid column name 'PackageHeightCm'`
- `Invalid column name 'PackageLengthCm'`
- `Invalid column name 'PackageWidthCm'`
- `Invalid column name 'Unit'`
- `Invalid column name 'WarrantyMonths'`

## Cách bản vá hoạt động

Khi ứng dụng khởi động, `MarketplaceProductSchemaInstaller` chạy trước controller và trigger giá. Installer dùng `COL_LENGTH` và `OBJECT_ID` nên:

- chỉ thêm cột hoặc bảng đang thiếu;
- không xóa và không ghi đè dữ liệu hiện có;
- có thể chạy lại nhiều lần;
- xác minh schema sau khi hoàn tất;
- dừng ứng dụng với thông báo rõ ràng nếu tài khoản SQL Server không có quyền `ALTER` hoặc `CREATE TABLE`.

## Cách chạy

1. Sao lưu database.
2. Giải nén ZIP vào thư mục chứa `TMDT1_TH.sln` và chọn ghi đè.
3. Xóa `TMDT1_TH/bin` và `TMDT1_TH/obj`.
4. Rebuild Solution.
5. Chạy ứng dụng một lần. Schema sẽ được sửa tự động trước khi website nhận request.

Không cần tạo migration mới chỉ để sửa lỗi schema này.

## Trường hợp tài khoản SQL không có quyền thay đổi schema

Mở SSMS, chọn đúng database `TMDT1_TH_DB`, sau đó chạy:

```text
TMDT1_TH/Data/Database/Scripts/RepairMarketplaceProductSchema.sql
```

Sau đó khởi động lại ứng dụng.

## Commit đề xuất

```text
fix(product-schema): auto-repair marketplace product and pricing database schema
```
