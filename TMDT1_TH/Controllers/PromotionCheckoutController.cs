using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TMDT1_TH.Infrastructure.Pricing;

namespace TMDT1_TH.Controllers;

[Authorize]
[Route("thanh-toan/khuyen-mai")]
public sealed class PromotionCheckoutController(
    PromotionService promotionService,
    PromotionCartPreviewResolver cartResolver)
    : Controller
{
    private readonly PromotionService _promotionService =
        promotionService;

    private readonly PromotionCartPreviewResolver _cartResolver =
        cartResolver;

    [HttpPost("kiem-tra")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Preview(
        string? code,
        CancellationToken cancellationToken)
    {
        var cart =
            await _cartResolver.ResolveAsync(
                cancellationToken);

        if (!cart.IsValid)
        {
            return BadRequest(new
            {
                message = string.Join(
                    " ",
                    cart.Errors)
            });
        }

        var result =
            await _promotionService.ResolveAsync(
                code,
                cart.Lines,
                cart.MarketId,
                StorePriceClock.Now,
                cancellationToken);

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
            currencyCode =
                cart.CurrencyCode,
            message =
                $"Đã áp dụng {result.Name} cho " +
                $"{result.ScopeName?.ToLowerInvariant()}."
        });
    }
}
