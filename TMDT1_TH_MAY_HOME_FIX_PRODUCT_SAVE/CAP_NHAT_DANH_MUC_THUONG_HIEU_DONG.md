# Cập nhật danh mục và thương hiệu động trong form sản phẩm

## Chức năng

- Form **Thêm sản phẩm** lấy danh mục trực tiếp từ bảng `Categories`.
- Form **Thêm sản phẩm** lấy thương hiệu trực tiếp từ bảng `Brands`.
- Danh mục vừa thêm sẽ xuất hiện khi đang ở trạng thái **Đang hiển thị**.
- Thương hiệu vừa thêm sẽ xuất hiện khi đang ở trạng thái **Đang hoạt động**.
- Danh mục con được hiển thị theo dạng `Danh mục cha › Danh mục con`.
- Khi dữ liệu POST không hợp lệ, danh sách lựa chọn được nạp lại, không bị mất dropdown.
- Form kiểm tra danh mục/thương hiệu có còn tồn tại và hoạt động trước khi chấp nhận.

## Cài đặt

1. Đóng Visual Studio.
2. Giải nén ZIP tại thư mục chứa `TMDT1_TH.sln`.
3. Chọn **Replace/Ghi đè**.
4. Mở lại project và Build.

Không cần tạo migration vì không thay đổi Models hoặc cấu trúc database.
