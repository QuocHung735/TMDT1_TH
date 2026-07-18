# Thay đổi giai đoạn 2

## File chính được thêm

- `Infrastructure/SlugHelper.cs`
- `Areas/Admin/ViewModels/CatalogAdminViewModels.cs`

## File chính được cập nhật

- `Areas/Admin/Controllers/CategoriesController.cs`
- `Areas/Admin/Controllers/BrandsController.cs`
- `Areas/Admin/Controllers/DashboardController.cs`
- `Areas/Admin/ViewModels/AdminUiModels.cs`
- `Areas/Admin/Views/Categories/Index.cshtml`
- `Areas/Admin/Views/Brands/Index.cshtml`
- `Areas/Admin/Views/Dashboard/Index.cshtml`
- `Views/Shared/_AdminToast.cshtml`
- `wwwroot/admin/css/admin.css`
- `wwwroot/admin/js/admin.js`

## Không thay đổi schema

Các entity và relationship vẫn sử dụng schema của gói Models. Vì vậy database đã được tạo trước đó không cần migration mới cho giai đoạn này.
