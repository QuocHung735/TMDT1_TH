# Giai đoạn 3 – CRUD sản phẩm, biến thể và bảng giá

## Chức năng đã triển khai

- Danh sách sản phẩm lấy trực tiếp từ SQL Server.
- Tìm kiếm theo tên hoặc SKU.
- Lọc theo danh mục, thương hiệu và trạng thái.
- Danh mục và thương hiệu trong form lấy trực tiếp từ database.
- Thêm và chỉnh sửa sản phẩm thật.
- Tự sinh slug không trùng.
- Tự sinh SKU sản phẩm nếu để trống.
- Upload ảnh đại diện vào `wwwroot/uploads/products`.
- Hỗ trợ sản phẩm đơn hoặc sản phẩm có biến thể.
- Hỗ trợ tối đa hai thuộc tính trong giao diện hiện tại.
- Tự sinh tổ hợp biến thể, CombinationKey và SKU biến thể.
- Lưu tồn kho sản phẩm hoặc tồn kho mặc định cho mỗi biến thể.
- Lưu ba giá: giá vốn, giá niêm yết và giá bán.
- Chọn thị trường và thời gian áp dụng giá.
- Để trống ngày kết thúc để áp dụng vô hạn.
- Bật hoặc tạm ẩn sản phẩm.
- Xóa mềm sản phẩm, giữ lịch sử giá.

## Cài đặt

1. Đóng Visual Studio.
2. Giải nén ZIP tại thư mục chứa `TMDT1_TH.sln`.
3. Chọn Replace/Ghi đè.
4. Mở lại project và chạy Build.
5. Chạy website, vào `/Admin/Products`.

## Database

Bản cập nhật này không thêm cột hoặc bảng mới nên không cần tạo migration.

Không chạy lại `Add-Migration` nếu database hiện tại đã được tạo từ bộ Models trước đó.

## Lưu ý khi sửa biến thể

Khi lưu lại sản phẩm có biến thể, hệ thống sẽ:

1. Xóa liên kết thuộc tính của các biến thể cũ.
2. Đánh dấu xóa mềm các biến thể cũ.
3. Tạo lại thuộc tính, giá trị và tổ hợp biến thể mới.
4. Giữ nguyên lịch sử giá đã phát sinh trước đó.

Giai đoạn tiếp theo nên triển khai quản lý giá riêng từng biến thể và chỉnh tồn kho riêng từng SKU.
