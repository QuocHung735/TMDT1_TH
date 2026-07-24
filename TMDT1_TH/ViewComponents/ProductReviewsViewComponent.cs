using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TMDT1_TH.Data;
using TMDT1_TH.Domain.Enums;
using TMDT1_TH.ViewModels.Storefront;

namespace TMDT1_TH.ViewComponents;

public sealed class ProductReviewsViewComponent(
    ApplicationDbContext db) : ViewComponent
{
    private readonly ApplicationDbContext _db = db;

    public async Task<IViewComponentResult> InvokeAsync(
        int productId)
    {
        var approvedQuery = _db.ProductReviews
            .AsNoTracking()
            .Where(x =>
                x.ProductId == productId &&
                x.Status == ProductReviewStatus.Approved);

        var reviewCount = await approvedQuery.CountAsync();

        var averageRating = reviewCount == 0
            ? 0
            : await approvedQuery.AverageAsync(x => x.Rating);

        var rows = await approvedQuery
            .OrderByDescending(x => x.CreatedAt)
            .Take(20)
            .Select(x => new
            {
                x.Rating,
                x.Title,
                x.Comment,
                x.CustomerDisplayName,
                VariantName = x.ProductVariant != null
                    ? x.ProductVariant.Name
                    : null,
                x.AdminReply,
                x.CreatedAt
            })
            .ToListAsync();

        var model = new StoreProductReviewsViewModel
        {
            ReviewCount = reviewCount,
            AverageRating = Math.Round(
                Convert.ToDecimal(averageRating),
                1),
            Items = rows
                .Select(x => new StoreProductReviewItemViewModel
                {
                    Rating = x.Rating,
                    Title = x.Title,
                    Comment = x.Comment,
                    CustomerDisplayName =
                        MaskCustomerName(x.CustomerDisplayName),
                    VariantName = x.VariantName,
                    AdminReply = x.AdminReply,
                    CreatedAt = x.CreatedAt
                })
                .ToList()
        };

        return View(model);
    }

    private static string MaskCustomerName(string value)
    {
        var parts = value
            .Split(
                ' ',
                StringSplitOptions.RemoveEmptyEntries);

        if (parts.Length == 0)
            return "Khách hàng";

        if (parts.Length == 1)
            return parts[0];

        var lastPart = parts[^1];
        var lastInitial = lastPart.Length > 0
            ? char.ToUpperInvariant(lastPart[0])
            : '*';

        return $"{parts[0]} {lastInitial}.";
    }
}
