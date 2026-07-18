# Mô hình dữ liệu thương mại điện tử

```text
Category 1 ─── n Category (danh mục cha/con)
Category 1 ─── n Product
Brand    1 ─── n Product

Product  1 ─── n ProductImage
Product  1 ─── n ProductOption
ProductOption 1 ─── n ProductOptionValue
Product  1 ─── n ProductVariant
ProductVariant n ─── n ProductOptionValue
ProductVariant 1 ─── n ProductImage

Market  1 ─── n PriceSchedule
Product 0..1 ─── n PriceSchedule
ProductVariant 0..1 ─── n PriceSchedule

PriceSchedule ──trigger──> PriceHistory
```

## Mục tiêu giá

`PriceSchedule` bắt buộc chọn đúng một mục tiêu:

```text
ProductId có giá trị, ProductVariantId null
hoặc
ProductId null, ProductVariantId có giá trị
```

## Khoảng thời gian

```text
ValidFrom: thời điểm bắt đầu
ValidTo: thời điểm kết thúc
ValidTo = null: vô hạn
```

Trigger kiểm tra hai lịch giá đang hoạt động không được giao nhau khi cùng:

- sản phẩm hoặc biến thể;
- thị trường.
