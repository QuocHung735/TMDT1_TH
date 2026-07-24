using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TMDT1_TH.Data;
using TMDT1_TH.Domain.Entities;
using TMDT1_TH.Domain.Enums;
using TMDT1_TH.Domain.Identity;
using TMDT1_TH.ViewModels.Storefront;

namespace TMDT1_TH.Controllers;

[Authorize]
[Route("tai-khoan/danh-gia")]
public sealed class MyReviewsController(
    ApplicationDbContext db,
    UserManager<ApplicationUser> userManager,
    ILogger<MyReviewsController> logger) : Controller
{
    private readonly ApplicationDbContext _db = db;
    private readonly UserManager<ApplicationUser> _userManager =
        userManager;
    private readonly ILogger<MyReviewsController> _logger = logger;

    [HttpGet("")]
    public async Task<IActionResult> Index()
    {
        var userId = CurrentUserId();

        var purchasedItems = await _db.OrderItems
            .AsNoTracking()
            .Where(x =>
                x.Order.CustomerUserId == userId &&
                x.Order.Status == OrderStatus.Completed &&
                x.ProductId.HasValue &&
                x.Product != null &&
                !x.Product.IsDeleted)
            .OrderByDescending(x =>
                x.Order.CompletedAt ?? x.Order.CreatedAt)
            .Select(x => new PurchasedItemRow(
                x.Id,
                x.Order.OrderNumber,
                x.Order.CompletedAt ?? x.Order.CreatedAt,
                x.ProductName,
                x.VariantName,
                x.Product!.Slug,
                x.ImageUrl))
            .ToListAsync();

        var orderItemIds = purchasedItems
            .Select(x => x.OrderItemId)
            .ToList();

        var reviews = orderItemIds.Count == 0
            ? new Dictionary<int, ExistingReviewRow>()
            : await _db.ProductReviews
                .AsNoTracking()
                .Where(x =>
                    x.CustomerUserId == userId &&
                    orderItemIds.Contains(x.OrderItemId))
                .Select(x => new ExistingReviewRow(
                    x.OrderItemId,
                    x.Rating,
                    x.Status,
                    x.CreatedAt))
                .ToDictionaryAsync(x => x.OrderItemId);

        var items = purchasedItems
            .Select(item =>
            {
                reviews.TryGetValue(
                    item.OrderItemId,
                    out var review);

                return new CustomerReviewListItemViewModel
                {
                    OrderItemId = item.OrderItemId,
                    OrderNumber = item.OrderNumber,
                    CompletedAt = item.CompletedAt,
                    ProductName = item.ProductName,
                    VariantName = item.VariantName,
                    ProductSlug = item.ProductSlug,
                    ImageUrl = item.ImageUrl,
                    CanReview = review is null,
                    Rating = review?.Rating,
                    Status = review?.Status,
                    StatusName = review is null
                        ? null
                        : ProductReviewDisplay.StatusName(review.Status),
                    StatusClass = review is null
                        ? null
                        : ProductReviewDisplay.StatusClass(review.Status),
                    ReviewedAt = review?.CreatedAt
                };
            })
            .ToList();

        var model = new CustomerReviewsPageViewModel
        {
            Items = items,
            ReviewableCount = items.Count(x => x.CanReview),
            PendingCount = items.Count(x =>
                x.Status == ProductReviewStatus.Pending),
            ApprovedCount = items.Count(x =>
                x.Status == ProductReviewStatus.Approved)
        };

        return View(model);
    }

    [HttpGet("tao/{orderItemId:int}")]
    public async Task<IActionResult> Create(int orderItemId)
    {
        var context =
            await LoadPurchasedItemAsync(orderItemId);

        if (context is null)
            return NotFound();

        var exists = await _db.ProductReviews
            .AsNoTracking()
            .AnyAsync(x => x.OrderItemId == orderItemId);

        if (exists)
        {
            TempData["ReviewMessage"] =
                "Sản phẩm trong đơn này đã được đánh giá.";

            return RedirectToAction(nameof(Index));
        }

        return View(BuildCreateModel(context));
    }

    // Route "gui" tách riêng khỏi GET "tao/{orderItemId}" để form
    // không sinh nhầm URL và bị lỗi 405. Route "tao" được giữ lại
    // để tương thích với form cũ còn trong cache.
    [HttpPost("gui")]
    [HttpPost("tao")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Submit(
        CreateProductReviewViewModel model)
    {
        model.Title = NullIfWhiteSpace(model.Title);
        model.Comment = model.Comment?.Trim() ?? string.Empty;

        ModelState.Clear();
        TryValidateModel(model);

        var context =
            await LoadPurchasedItemAsync(model.OrderItemId);

        if (context is null)
        {
            ModelState.AddModelError(
                string.Empty,
                "Không tìm thấy sản phẩm hợp lệ trong đơn hàng đã hoàn thành.");

            return View("Create", model);
        }

        ApplyPurchasedItem(model, context);

        if (!ModelState.IsValid)
            return View("Create", model);

        var exists = await _db.ProductReviews
            .AsNoTracking()
            .AnyAsync(x => x.OrderItemId == model.OrderItemId);

        if (exists)
        {
            ModelState.AddModelError(
                string.Empty,
                "Sản phẩm trong đơn này đã được đánh giá.");

            return View("Create", model);
        }

        var user =
            await _userManager.GetUserAsync(User);

        if (user is null || !user.IsActive)
            return Challenge();

        var review = new ProductReview
        {
            ProductId = context.ProductId,
            ProductVariantId = context.ProductVariantId,
            OrderItemId = context.OrderItemId,
            CustomerUserId = user.Id,
            Rating = model.Rating,
            Title = model.Title,
            Comment = model.Comment,
            CustomerDisplayName =
                string.IsNullOrWhiteSpace(user.FullName)
                    ? user.Email
                        ?? user.UserName
                        ?? "Khách hàng"
                    : user.FullName,
            Status = ProductReviewStatus.Pending,
            CreatedBy = user.Email
                ?? user.UserName
                ?? "Customer"
        };

        try
        {
            _db.ProductReviews.Add(review);
            await _db.SaveChangesAsync();

            TempData["ReviewMessage"] =
                "Đã gửi đánh giá. Nội dung sẽ hiển thị sau khi Admin duyệt.";

            return RedirectToAction(nameof(Index));
        }
        catch (DbUpdateException exception)
        {
            _logger.LogWarning(
                exception,
                "Không thể tạo đánh giá cho OrderItem {OrderItemId}.",
                model.OrderItemId);

            ModelState.AddModelError(
                string.Empty,
                "Không thể lưu đánh giá hoặc sản phẩm đã được đánh giá.");

            return View("Create", model);
        }
    }

    private async Task<PurchasedItemContext?> LoadPurchasedItemAsync(
        int orderItemId)
    {
        var userId = CurrentUserId();

        return await _db.OrderItems
            .AsNoTracking()
            .Where(x =>
                x.Id == orderItemId &&
                x.Order.CustomerUserId == userId &&
                x.Order.Status == OrderStatus.Completed &&
                x.ProductId.HasValue &&
                x.Product != null &&
                !x.Product.IsDeleted)
            .Select(x => new PurchasedItemContext(
                x.Id,
                x.ProductId!.Value,
                x.ProductVariantId,
                x.Order.OrderNumber,
                x.ProductName,
                x.VariantName,
                x.ImageUrl))
            .FirstOrDefaultAsync();
    }

    private static CreateProductReviewViewModel BuildCreateModel(
        PurchasedItemContext context)
    {
        var model = new CreateProductReviewViewModel
        {
            Rating = 5
        };

        ApplyPurchasedItem(model, context);
        return model;
    }

    private static void ApplyPurchasedItem(
        CreateProductReviewViewModel model,
        PurchasedItemContext context)
    {
        model.OrderItemId = context.OrderItemId;
        model.OrderNumber = context.OrderNumber;
        model.ProductName = context.ProductName;
        model.VariantName = context.VariantName;
        model.ImageUrl = context.ImageUrl;
    }

    private int CurrentUserId()
    {
        var value = _userManager.GetUserId(User);

        return int.TryParse(value, out var userId)
            ? userId
            : throw new InvalidOperationException(
                "Không xác định được tài khoản hiện tại.");
    }

    private static string? NullIfWhiteSpace(string? value) =>
        string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim();

    private sealed record PurchasedItemRow(
        int OrderItemId,
        string OrderNumber,
        DateTime CompletedAt,
        string ProductName,
        string? VariantName,
        string ProductSlug,
        string? ImageUrl);

    private sealed record ExistingReviewRow(
        int OrderItemId,
        int Rating,
        ProductReviewStatus Status,
        DateTime CreatedAt);

    private sealed record PurchasedItemContext(
        int OrderItemId,
        int ProductId,
        int? ProductVariantId,
        string OrderNumber,
        string ProductName,
        string? VariantName,
        string? ImageUrl);
}
