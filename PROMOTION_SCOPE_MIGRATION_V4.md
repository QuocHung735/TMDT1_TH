# PROMOTION SCOPE MIGRATION V4

## Lỗi của V3

PowerShell có thể trả về một đối tượng file đơn lẻ thay vì một mảng.

Khi bật:

```powershell
Set-StrictMode -Version Latest
```

đối tượng file đơn không có thuộc tính:

```powershell
.Count
```

V3 vì vậy dừng tại bước tìm migration.

## V4 đã sửa

V4 luôn ép kết quả tìm kiếm thành mảng:

```powershell
$scopeMigrations = @(
    Get-ChildItem ...
)
```

V4 vẫn thực hiện đầy đủ:

1. Tìm migration phạm vi khuyến mãi.
2. Sao lưu migration và snapshot.
3. Gỡ migration lỗi.
4. Đặt `ScopeType` mặc định bằng `AllProducts = 1`.
5. Tạo migration mới.
6. Xác minh `defaultValue: 1`.
7. Cập nhật database.
8. Build và kiểm tra model.

## Cách chạy

Không khôi phục file backup của V2 hoặc V3.

1. Dừng website bằng `Shift + F5`.
2. Giải nén ZIP vào thư mục chứa `TMDT1_TH.sln`.
3. Chọn Replace/Ghi đè.
4. Chạy:

```cmd
tools\Run-PromotionScopeMigration-V4.cmd
```

Kết quả:

```text
PROMOTION SCOPE MIGRATION V4 COMPLETED SUCCESSFULLY
```

## Commit

```text
fix(promotion): repair scoped promotion migration
```

Không commit:

```text
tools/Fix-PromotionScopeMigration-V4.ps1
tools/Run-PromotionScopeMigration-V4.cmd
PROMOTION_SCOPE_MIGRATION_V4_LOG.txt
PROMOTION_SCOPE_MIGRATION_V4.md
promotion-scope-migration-v4-backup/
*.before-promotion-scope-v2.bak
```
