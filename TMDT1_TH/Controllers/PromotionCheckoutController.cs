using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TMDT1_TH.Data;
using TMDT1_TH.Infrastructure.Pricing;

namespace TMDT1_TH.Controllers;

[Authorize]
[Route("thanh-toan/khuyen-mai")]
public sealed class PromotionCheckoutController(
    ApplicationDbContext db,
    PromotionService promotionService) : Controller
{
    private readonly ApplicationDbContext _db = db;
    private readonly PromotionService _promotionService =
        promotionService;

    [HttpPost("kiem-tra")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Preview(
        string? code,
        List<int>? productIds,
        List<decimal>? lineTotals)
    {
        var marketId = await _db.Markets
            .AsNoTracking()
            .Where(x => x.IsActive)
            .OrderByDescending(x => x.IsDefault)
            .ThenBy(x => x.Id)
            .Select(x => (int?)x.Id)
            .FirstOrDefaultAsync();

        if (!marketId.HasValue)
        {
            return BadRequest(new
            {
                message =
                    "Chưa có thị trường đang hoạt động."
            });
        }

        var lines =
            BuildLines(productIds, lineTotals);

        if (lines.Count == 0)
        {
            return BadRequest(new
            {
                message =
                    "Không xác định được sản phẩm trong giỏ hàng."
            });
        }

        var result =
            await _promotionService.ResolveAsync(
                code,
                lines,
                marketId.Value,
                StorePriceClock.Now);

        if (!result.IsValid)
        {
            return BadRequest(new
            {
                message = result.Error
            });
        }

        return Json(new
        {
            promotionName = result.Name,
            code = result.Code,
            scopeName = result.ScopeName,
            eligibleSubtotal =
                result.EligibleSubtotal,
            discountAmount =
                result.DiscountAmount,
            message =
                $"Đã áp dụng {result.Name} cho " +
                $"{result.ScopeName.ToLowerInvariant()}."
        });
    }

    private static List<PromotionCartLine> BuildLines(
        IReadOnlyList<int>? productIds,
        IReadOnlyList<decimal>? lineTotals)
    {
        if (productIds is null ||
            lineTotals is null)
        {
            return new List<PromotionCartLine>();
        }

        var count =
            Math.Min(
                productIds.Count,
                lineTotals.Count);

        var lines =
            new List<PromotionCartLine>();

        for (var index = 0;
             index < count;
             index++)
        {
            if (productIds[index] <= 0 ||
                lineTotals[index] <= 0)
            {
                continue;
            }

            lines.Add(
                new PromotionCartLine(
                    productIds[index],
                    lineTotals[index]));
        }

        return lines;
    }
}
