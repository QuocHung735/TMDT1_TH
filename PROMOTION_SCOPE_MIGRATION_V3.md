# SỬA MIGRATION PHẠM VI KHUYẾN MÃI V3

## Nguyên nhân có khả năng cao

Migration V2 thêm cột:

```text
ScopeType
```

vào bảng `Promotions`.

Vì đây là cột `int NOT NULL`, EF thường sinh:

```csharp
defaultValue: 0
```

Nhưng check constraint chỉ cho phép:

```text
1 = Tất cả sản phẩm
2 = Sản phẩm cụ thể
3 = Danh mục cụ thể
4 = Thương hiệu cụ thể
```

Các khuyến mãi cũ nhận giá trị `0`, nên SQL Server từ chối migration.

## V3 thực hiện

1. Tìm migration `AddPromotionScope_*` vừa tạo.
2. Sao lưu migration và snapshot.
3. Gỡ migration lỗi.
4. Đặt mặc định:

```csharp
PromotionScopeType.AllProducts
```

tức giá trị `1`.

5. Tạo lại migration.
6. Xác minh migration có `defaultValue: 1`.
7. Cập nhật database.
8. Build và kiểm tra model lần cuối.

## Cách chạy

Không khôi phục các file `.bak` của V2.

1. Dừng website bằng `Shift + F5`.
2. Giải nén ZIP vào thư mục chứa `TMDT1_TH.sln`.
3. Chọn Replace/Ghi đè.
4. Chạy:

```cmd
tools\Run-PromotionScopeMigration-V3.cmd
```

Kết quả:

```text
PROMOTION SCOPE MIGRATION V3 COMPLETED SUCCESSFULLY
```

## Sau khi thành công

- Mở Admin → Quản lý giá → Khuyến mãi.
- Khuyến mãi cũ được hiểu là `Toàn bộ sản phẩm`.
- Chỉnh lại phạm vi cho từng chương trình nếu cần.
- Tạo khuyến mãi mới và kiểm tra mã tự sinh.
- Test checkout theo sản phẩm, danh mục và thương hiệu.

## Commit

Commit migration V3 mới và source đã sửa:

```text
fix(promotion): use valid default scope for existing promotions
```

Không commit:

```text
tools/Fix-PromotionScopeMigration-V3.ps1
tools/Run-PromotionScopeMigration-V3.cmd
PROMOTION_SCOPE_MIGRATION_V3_LOG.txt
PROMOTION_SCOPE_MIGRATION_V3.md
promotion-scope-migration-v3-backup/
*.before-promotion-scope-v2.bak
```
