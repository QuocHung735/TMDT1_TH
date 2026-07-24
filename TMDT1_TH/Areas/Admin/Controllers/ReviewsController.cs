using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using TMDT1_TH.Areas.Admin.ViewModels;
using TMDT1_TH.Data;
using TMDT1_TH.Domain.Enums;
using TMDT1_TH.ViewModels.Storefront;

namespace TMDT1_TH.Areas.Admin.Controllers;

[Area("Admin")]
public sealed class ReviewsController(
    ApplicationDbContext db) : Controller
{
    private readonly ApplicationDbContext _db = db;

    public async Task<IActionResult> Index(
        string? q,
        ProductReviewStatus? status,
        int? rating)
    {
        q = string.IsNullOrWhiteSpace(q)
            ? null
            : q.Trim();

        if (rating is < 1 or > 5)
            rating = null;

        var query = _db.ProductReviews
            .AsNoTracking()
            .Include(x => x.Product)
            .Include(x => x.ProductVariant)
            .Include(x => x.OrderItem)
                .ThenInclude(x => x.Order)
            .AsQueryable();

        if (status.HasValue)
            query = query.Where(x => x.Status == status.Value);

        if (rating.HasValue)
            query = query.Where(x => x.Rating == rating.Value);

        if (q is not null)
        {
            query = query.Where(x =>
                x.Product.Name.Contains(q) ||
                x.CustomerDisplayName.Contains(q) ||
                x.OrderItem.Order.OrderNumber.Contains(q) ||
                x.Comment.Contains(q) ||
                (x.Title != null && x.Title.Contains(q)));
        }

        var statusCounts = await _db.ProductReviews
            .AsNoTracking()
            .GroupBy(x => x.Status)
            .Select(group => new
            {
                Status = group.Key,
                Count = group.Count()
            })
            .ToDictionaryAsync(x => x.Status, x => x.Count);

        var reviews = await query
            .OrderByDescending(x => x.CreatedAt)
            .Take(300)
            .ToListAsync();

        var model = new AdminReviewsViewModel
        {
            Query = q,
            Status = status,
            Rating = rating,
            TotalCount = statusCounts.Values.Sum(),
            PendingCount =
                statusCounts.GetValueOrDefault(
                    ProductReviewStatus.Pending),
            ApprovedCount =
                statusCounts.GetValueOrDefault(
                    ProductReviewStatus.Approved),
            RejectedCount =
                statusCounts.GetValueOrDefault(
                    ProductReviewStatus.Rejected),
            HiddenCount =
                statusCounts.GetValueOrDefault(
                    ProductReviewStatus.Hidden),
            Items = reviews
                .Select(x => new AdminReviewListItemViewModel
                {
                    Id = x.Id,
                    ProductName = x.Product.Name,
                    VariantName = x.ProductVariant?.Name,
                    CustomerName = x.CustomerDisplayName,
                    OrderNumber = x.OrderItem.Order.OrderNumber,
                    Rating = x.Rating,
                    CommentPreview = Preview(x.Comment, 110),
                    Status = x.Status,
                    StatusName =
                        ProductReviewDisplay.StatusName(x.Status),
                    StatusClass =
                        ProductReviewDisplay.StatusClass(x.Status),
                    CreatedAt = x.CreatedAt
                })
                .ToList()
        };

        return View(model);
    }

    public async Task<IActionResult> Details(int id)
    {
        var review = await _db.ProductReviews
            .AsNoTracking()
            .Include(x => x.Product)
            .Include(x => x.ProductVariant)
            .Include(x => x.CustomerUser)
            .Include(x => x.OrderItem)
                .ThenInclude(x => x.Order)
            .FirstOrDefaultAsync(x => x.Id == id);

        if (review is null)
            return NotFound();

        return View(BuildDetailsModel(review));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Moderate(
        int id,
        ProductReviewStatus status,
        string? adminReply)
    {
        var allowedStatuses = new[]
        {
            ProductReviewStatus.Pending,
            ProductReviewStatus.Approved,
            ProductReviewStatus.Rejected,
            ProductReviewStatus.Hidden
        };

        if (!allowedStatuses.Contains(status))
            return BadRequest();

        adminReply = string.IsNullOrWhiteSpace(adminReply)
            ? null
            : adminReply.Trim();

        if (adminReply?.Length > 1000)
        {
            TempData["Error"] =
                "Phản hồi của Admin không được vượt quá 1000 ký tự.";

            return RedirectToAction(
                nameof(Details),
                new { id });
        }

        var review = await _db.ProductReviews
            .FirstOrDefaultAsync(x => x.Id == id);

        if (review is null)
            return NotFound();

        review.Status = status;
        review.AdminReply = adminReply;
        review.ModeratedAt = status == ProductReviewStatus.Pending
            ? null
            : DateTime.UtcNow;
        review.AdminRepliedAt = adminReply is null
            ? null
            : DateTime.UtcNow;
        review.UpdatedBy =
            User.Identity?.Name ?? "Admin";

        await _db.SaveChangesAsync();

        TempData["Success"] =
            $"Đánh giá đã chuyển sang " +
            $"{ProductReviewDisplay.StatusName(status)}.";

        return RedirectToAction(
            nameof(Details),
            new { id });
    }

    private static AdminReviewDetailsViewModel BuildDetailsModel(
        TMDT1_TH.Domain.Entities.ProductReview review)
    {
        return new AdminReviewDetailsViewModel
        {
            Id = review.Id,
            ProductName = review.Product.Name,
            ProductSlug = review.Product.Slug,
            VariantName = review.ProductVariant?.Name,
            ImageUrl = review.OrderItem.ImageUrl,
            CustomerName = review.CustomerDisplayName,
            CustomerEmail = review.CustomerUser.Email,
            OrderNumber = review.OrderItem.Order.OrderNumber,
            Rating = review.Rating,
            Title = review.Title,
            Comment = review.Comment,
            CreatedAt = review.CreatedAt,
            Status = review.Status,
            StatusName =
                ProductReviewDisplay.StatusName(review.Status),
            StatusClass =
                ProductReviewDisplay.StatusClass(review.Status),
            AdminReply = review.AdminReply,
            ModeratedAt = review.ModeratedAt,
            StatusOptions = Enum
                .GetValues<ProductReviewStatus>()
                .Select(status => new SelectListItem
                {
                    Value = ((int)status).ToString(),
                    Text = ProductReviewDisplay.StatusName(status),
                    Selected = status == review.Status
                })
                .ToList()
        };
    }

    private static string Preview(
        string value,
        int maxLength)
    {
        if (value.Length <= maxLength)
            return value;

        return value[..maxLength].TrimEnd() + "…";
    }
}
