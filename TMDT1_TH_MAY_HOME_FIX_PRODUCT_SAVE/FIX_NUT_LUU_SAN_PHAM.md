# Sửa lỗi nút Lưu sản phẩm

Bản cập nhật này sửa luồng submit của trang `/Admin/Products/Create` và `/Admin/Products/Edit`.

## Thay đổi

- Nút trên đầu trang gọi `requestSubmit()` tới đúng form.
- Có nút lưu dự phòng nằm trực tiếp bên trong form.
- Hiển thị toàn bộ lỗi validation, kể cả lỗi theo từng trường.
- Tự cuộn và focus vào trường HTML không hợp lệ.
- Hiển thị cảnh báo rõ nếu chưa có danh mục, thương hiệu hoặc thị trường đang hoạt động.
- Chống bấm lưu nhiều lần trong lúc gửi form.

Không thay đổi Models hoặc cấu trúc database, vì vậy không cần migration mới.
