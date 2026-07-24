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
        decimal subtotal)
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

        var result =
            await _promotionService.ResolveAsync(
                code,
                subtotal,
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
            discountAmount = result.DiscountAmount,
            message =
                $"Đã áp dụng {result.Name}."
        });
    }
}
