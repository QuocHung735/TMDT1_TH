# KHUYẾN MÃI V2 — PHẠM VI RÕ RÀNG VÀ MÃ TỰ SINH

## Phạm vi áp dụng

Mỗi chương trình chọn đúng một loại:

```text
1. Toàn bộ sản phẩm
2. Sản phẩm cụ thể
3. Danh mục cụ thể
4. Thương hiệu cụ thể
```

Có thể chọn nhiều đối tượng trong cùng loại.

Ví dụ:

```text
Loại phạm vi: Thương hiệu cụ thể
Thương hiệu: Lego, Hasbro
```

Mã chỉ giảm cho các dòng hàng thuộc Lego hoặc Hasbro.

## Cách tính

```text
Tổng đơn tối thiểu:
Kiểm tra trên toàn bộ giá trị đơn hàng.

Giá trị được giảm:
Chỉ tính trên các sản phẩm thuộc phạm vi khuyến mãi.
```

Ví dụ:

```text
Đồ chơi Lego: 500.000đ
Nồi cơm điện: 1.000.000đ
Mã giảm 20% cho thương hiệu Lego
```

Khoản giảm:

```text
20% × 500.000đ = 100.000đ
```

Không tính 20% trên 1.500.000đ.

## Mã tự sinh

Khi bấm `Tạo khuyến mãi`, hệ thống tự sinh dạng:

```text
KM-260725-A7K9
```

Admin chỉ xem, không nhập hoặc sửa thủ công.

Khi chỉnh sửa chương trình, mã cũ được giữ nguyên.

## Cài đặt

Bản này nâng cấp module khuyến mãi đã cài trước đó.

1. Dừng website bằng `Shift + F5`.
2. Giải nén ZIP tại thư mục chứa `TMDT1_TH.sln`.
3. Chọn Replace/Ghi đè.
4. Chạy:

```cmd
tools\Run-PromotionScope-V2.cmd
```

Kết quả:

```text
PROMOTION SCOPE V2 COMPLETED SUCCESSFULLY
```

Script tự tạo migration mới và cập nhật database.

## Test bắt buộc

### Toàn bộ sản phẩm

- Chọn `Toàn bộ sản phẩm`.
- Mã phải giảm trên tổng hàng trong giỏ.

### Sản phẩm cụ thể

- Chọn một sản phẩm A.
- Giỏ có A và B.
- Mã chỉ giảm phần tiền của A.

### Danh mục cụ thể

- Chọn danh mục Đồ chơi.
- Giỏ có đồ chơi và đồ gia dụng.
- Mã chỉ giảm phần đồ chơi.

### Thương hiệu cụ thể

- Chọn thương hiệu Lego.
- Giỏ có Lego và thương hiệu khác.
- Mã chỉ giảm phần Lego.

### Mã tự sinh

- Mở form tạo mới.
- Mã có dạng `KM-YYMMDD-XXXX`.
- Ô mã là readonly.
- Tạo hai chương trình liên tiếp phải có mã khác nhau.

## Commit

```text
feat(promotion): add scoped promotions and automatic coupon codes
```

Phải commit migration mới trong:

```text
TMDT1_TH/Migrations/
```

Không commit:

```text
tools/Install-PromotionScope-V2.ps1
tools/Run-PromotionScope-V2.cmd
PROMOTION_SCOPE_V2_LOG.txt
PROMOTION_SCOPE_V2.md
*.before-promotion-scope-v2.bak
```
