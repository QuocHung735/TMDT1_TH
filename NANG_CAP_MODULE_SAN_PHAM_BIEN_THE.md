# Nâng cấp module Sản phẩm & Biến thể

## 1. Lỗi giá trong ảnh đã được sửa

Lỗi trình duyệt:

```text
Please enter a valid value. The two nearest valid values are ...001
```

xuất hiện do ô giá dùng `min="1"` cùng `step="1000"`. Khi đó các giá hợp lệ là `1, 1001, 2001, ...`, nên các số tròn như `250000` bị từ chối trước khi form được gửi.

Bản này dùng:

```html
<input type="number" min="0" step="1" />
```

cho toàn bộ giá VND của sản phẩm và SKU biến thể.

## 2. Nghiệp vụ sản phẩm

- Một bài đăng tương ứng với một sản phẩm.
- Khác biệt về màu, kích thước, dung tích hoặc quy cách đóng gói được quản lý bằng biến thể.
- Tên sản phẩm từ 10 đến 250 ký tự.
- SKU sản phẩm tự sinh nếu để trống và không được trùng.
- SKU sản phẩm không được trùng với SKU biến thể.
- Danh mục và thương hiệu lấy trực tiếp từ database.
- Hỗ trợ mã model, đơn vị bán, bảo hành, xuất xứ, nhà sản xuất và địa chỉ chịu trách nhiệm.
- Hỗ trợ mô tả ngắn, mô tả chi tiết và thông số kỹ thuật có thứ tự.
- Hỗ trợ số lượng mua tối thiểu và tối đa mỗi đơn.
- Xóa mềm sản phẩm; không xóa lịch sử giá.

## 3. Hình ảnh

- Tối đa 9 hình ảnh cho một sản phẩm.
- JPG, JPEG, PNG hoặc WEBP.
- Tối đa 5 MB mỗi ảnh.
- Có một ảnh chính duy nhất.
- Ảnh bị xóa chỉ bị xóa khỏi ổ đĩa khi không còn sản phẩm nào dùng chung URL đó.
- Khi đăng bán phải có ít nhất một ảnh.
- Giao diện kiểm tra ảnh nhỏ hơn 600 x 600 px và cảnh báo trước khi gửi.

## 4. Phân loại và SKU biến thể

- Tối đa 2 tầng phân loại.
- Tối đa 20 giá trị mỗi tầng.
- Tối đa 100 tổ hợp SKU.
- Hai tầng phân loại không được trùng tên.
- Giá trị phân loại được loại bỏ phần tử rỗng và trùng lặp.
- Mỗi tổ hợp có:
  - SKU người bán;
  - barcode;
  - giá vốn;
  - giá niêm yết;
  - giá bán;
  - tồn kho;
  - ngưỡng cảnh báo tồn kho;
  - khối lượng;
  - trạng thái bán;
  - cờ biến thể mặc định.
- Chỉ một biến thể mặc định.
- SKU và barcode không trùng trong biểu mẫu hoặc database.
- Có thể đổi chéo SKU giữa hai biến thể trong cùng một lần lưu.
- Biến thể bị loại khỏi tổ hợp được xóa mềm và tắt lịch giá.

## 5. Giá và lịch giá

- Sản phẩm đơn lưu giá ở cấp sản phẩm.
- Sản phẩm có biến thể lưu giá riêng cho từng SKU.
- Giá vốn không âm.
- Giá niêm yết lớn hơn 0 khi đăng bán.
- Giá bán lớn hơn 0 và không vượt giá niêm yết.
- Giá áp dụng theo thị trường.
- Hỗ trợ thời gian bắt đầu, kết thúc hoặc vô hạn.
- Kiểm tra chồng khoảng thời gian ở controller và trigger SQL Server.
- Khi xóa giá khỏi bản nháp, lịch giá cũ được tắt để tránh giá cũ tiếp tục hoạt động.
- Lịch sử giá tiếp tục được trigger ghi nhận.

## 6. Kho và trạng thái

- Sản phẩm đơn quản lý tồn kho ở sản phẩm.
- Sản phẩm biến thể quản lý tồn kho theo từng SKU; tồn kho sản phẩm là tổng SKU đang hoạt động.
- Nếu sản phẩm đang bán nhưng tổng tồn kho bằng 0, trạng thái chuyển thành Hết hàng.
- Khi có tồn trở lại, trạng thái Hết hàng có thể chuyển lại Đang bán.
- Có ngưỡng cảnh báo tồn kho ở cả sản phẩm và SKU.

## 7. Điều kiện đăng bán

Khi trạng thái không phải Bản nháp, hệ thống kiểm tra:

- tên hợp lệ và không chứa nội dung quảng cáo hoặc URL;
- mô tả chi tiết tối thiểu 110 ký tự;
- có hình ảnh;
- có xuất xứ;
- có tên và địa chỉ nhà sản xuất hoặc đơn vị chịu trách nhiệm;
- có khối lượng và kích thước kiện hàng;
- có thị trường giá;
- sản phẩm đơn có giá hợp lệ;
- mọi SKU đang hoạt động có giá, SKU và khối lượng hợp lệ;
- có ít nhất một SKU hoạt động và đúng một SKU mặc định.

Có thể dùng **Lưu nháp** khi nội dung chưa hoàn thiện. Danh mục và thương hiệu vẫn phải tồn tại để dữ liệu không bị mồ côi.

## 8. Chức năng quản trị

- Danh sách lấy dữ liệu thật từ SQL Server.
- Tìm theo tên, SKU, model hoặc barcode.
- Lọc danh mục, thương hiệu, trạng thái.
- Hiển thị khoảng giá thấp nhất - cao nhất.
- Hiển thị tổng tồn kho.
- Chấm điểm độ hoàn thiện bài đăng.
- Chỉnh sửa, bật/tắt bán, nhân bản bản nháp và xóa mềm.
- Chuyển thị giá theo thị trường được chọn khi chỉnh sửa.

## 9. Cập nhật database bắt buộc

Bản này thêm cột vào `Products`, `ProductVariants` và thêm bảng `ProductSpecifications`.

Sao lưu database trước, sau đó chạy trong Package Manager Console:

```powershell
Add-Migration UpgradeMarketplaceProductModule
Update-Database
```

Không chạy migration thì ứng dụng sẽ báo lỗi thiếu cột hoặc thiếu bảng khi mở trang sản phẩm.

## 10. Kịch bản kiểm thử

1. Tạo sản phẩm đơn và lưu nháp khi chưa có giá.
2. Nhập giá `200000`, `400000`, `250000` và xác nhận trình duyệt không còn báo nearest valid values.
3. Bật Đang bán khi thiếu ảnh hoặc vận chuyển và xác nhận hiển thị lỗi cụ thể.
4. Tạo hai phân loại 3 x 2 và xác nhận sinh 6 SKU.
5. Nhập SKU trùng và xác nhận bị chặn.
6. Nhập giá bán lớn hơn giá niêm yết và xác nhận bị chặn.
7. Đổi chéo SKU giữa hai biến thể và lưu.
8. Xóa một giá trị phân loại và xác nhận SKU tương ứng được xóa mềm.
9. Đổi thị trường và xác nhận bảng giá đúng thị trường được tải.
10. Đưa toàn bộ tồn kho về 0 và xác nhận trạng thái Hết hàng.
