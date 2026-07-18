# Giai đoạn 5 – Thị trường dữ liệu thật và CSS thuần

## Dữ liệu thật từ SQL Server

Trang `/Admin/Markets` sử dụng trực tiếp các bảng:

- `Markets`
- `PriceSchedules`

Không có danh sách thị trường ghi cứng trong controller hoặc Razor. Tổng thị trường, số thị trường đang hoạt động, tổng lịch giá và số lịch giá đang áp dụng đều được truy vấn từ database.

## Chức năng thị trường

- Tìm kiếm theo mã, tên, tiền tệ hoặc quốc gia.
- Lọc trạng thái hoạt động.
- Thêm và chỉnh sửa thị trường.
- Chuẩn hóa mã thị trường, tiền tệ và quốc gia thành chữ hoa.
- Chống trùng mã thị trường.
- Đặt một thị trường làm mặc định.
- Tự kích hoạt khi đặt làm mặc định.
- Không cho tạm tắt hoặc xóa thị trường mặc định.
- Không cho xóa thị trường đã có lịch giá.
- Hiển thị tổng lịch giá và lịch giá đang áp dụng cho từng thị trường.

## CSS thuần, không Bootstrap

Giao diện chỉ dùng file nội bộ:

```text
wwwroot/admin/css/style.css
wwwroot/admin/css/icons.css
wwwroot/admin/js/admin.js
```

Đã loại bỏ:

- Bootstrap CSS.
- Bootstrap Icons.
- Google Fonts.
- Mọi đường dẫn CSS hoặc icon CDN.

Các component chính được đặt tên riêng:

```text
ui-button
ui-button--primary
ui-button--light
ui-button--danger
ui-select
ui-icon-button
ui-icon
```

`icons.css` sử dụng ký hiệu Unicode và CSS thuần, không tải font icon.

## Dữ liệu mẫu

Ứng dụng không còn gọi `HouseholdCatalogSeeder` khi khởi động. Danh mục, thương hiệu, sản phẩm, biến thể và giá chỉ hiển thị khi tồn tại trong SQL Server.

Ba thị trường nền tảng được khai báo bằng EF Core `HasData` trong migration ban đầu. Đây là các bản ghi thật trong bảng `Markets`, không phải dữ liệu giả trong View hoặc Controller.

## Migration

Giai đoạn này không thêm bảng hoặc cột mới, do đó database đã được tạo không cần migration mới.
