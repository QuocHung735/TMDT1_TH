using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using TMDT1_TH.Areas.Admin.Controllers;
using TMDT1_TH.Areas.Admin.ViewModels;
using TMDT1_TH.Data;
using TMDT1_TH.Domain.Entities;

namespace TMDT1_TH.Infrastructure.Pricing;

/// <summary>
/// Cho phép form sản phẩm áp dụng cùng một bộ giá cho nhiều thị trường
/// mà không thay đổi schema PriceSchedules hiện tại.
/// </summary>
public sealed class MultiMarketPriceFilter(
    ApplicationDbContext db,
    ILogger<MultiMarketPriceFilter> logger)
    : IAsyncActionFilter
{
    private readonly ApplicationDbContext _db = db;
    private readonly ILogger<MultiMarketPriceFilter> _logger = logger;

    public async Task OnActionExecutionAsync(
        ActionExecutingContext context,
        ActionExecutionDelegate next)
    {
        if (context.Controller is not ProductsController ||
            !string.Equals(
                context.ActionDescriptor.RouteValues["action"],
                "Save",
                StringComparison.OrdinalIgnoreCase) ||
            !context.ActionArguments.TryGetValue(
                "model",
                out var argument) ||
            argument is not ProductEditorViewModel model)
        {
            await next();
            return;
        }

        model.MarketIds ??= new List<int>();

        var selectedMarketIds = model.MarketIds
            .Where(x => x > 0)
            .Distinct()
            .ToList();

        if (model.MarketId.HasValue &&
            model.MarketId.Value > 0 &&
            !selectedMarketIds.Contains(
                model.MarketId.Value))
        {
            selectedMarketIds.Insert(
                0,
                model.MarketId.Value);
        }

        if (selectedMarketIds.Count > 0)
        {
            if (!model.MarketId.HasValue ||
                !selectedMarketIds.Contains(
                    model.MarketId.Value))
            {
                model.MarketId =
                    selectedMarketIds[0];
            }

            model.MarketIds =
                selectedMarketIds;

            var activeMarketIds =
                await _db.Markets
                    .AsNoTracking()
                    .Where(x =>
                        selectedMarketIds.Contains(
                            x.Id) &&
                        x.IsActive)
                    .Select(x => x.Id)
                    .ToListAsync();

            var invalidIds =
                selectedMarketIds
                    .Except(activeMarketIds)
                    .ToList();

            if (invalidIds.Count > 0 &&
                context.Controller is Controller controller)
            {
                controller.ModelState.AddModelError(
                    nameof(model.MarketId),
                    "Có thị trường không tồn tại hoặc đã bị tạm ẩn.");
            }
        }

        var executed = await next();

        if (executed.Exception is not null &&
            !executed.ExceptionHandled)
        {
            return;
        }

        if (context.Controller is not Controller mvcController ||
            !mvcController.ModelState.IsValid ||
            executed.Result is not RedirectToActionResult ||
            !model.MarketId.HasValue ||
            model.MarketIds.Count <= 1)
        {
            return;
        }

        try
        {
            var product = model.Id.HasValue
                ? await _db.Products
                    .IgnoreQueryFilters()
                    .FirstOrDefaultAsync(x =>
                        x.Id == model.Id.Value &&
                        !x.IsDeleted)
                : await _db.Products
                    .IgnoreQueryFilters()
                    .FirstOrDefaultAsync(x =>
                        x.Sku == model.Sku &&
                        !x.IsDeleted);

            if (product is null)
            {
                SetCopyWarning(
                    mvcController,
                    "Sản phẩm đã được lưu nhưng không xác định được sản phẩm để áp dụng giá cho các thị trường còn lại.");

                return;
            }

            var copiedCount =
                await CopyCurrentPriceSchedulesAsync(
                    product,
                    model.MarketId.Value,
                    model.MarketIds,
                    model.ValidFrom,
                    model.ValidTo,
                    mvcController.User.Identity?.Name
                        ?? "Admin");

            if (copiedCount > 0)
            {
                var currentMessage =
                    mvcController.TempData["Success"]
                        ?.ToString();

                mvcController.TempData["Success"] =
                    $"{currentMessage} Đã áp dụng bộ giá cho {model.MarketIds.Count} thị trường.";
            }
        }
        catch (Exception exception)
        {
            _logger.LogError(
                exception,
                "Không thể sao chép giá nhiều thị trường cho sản phẩm SKU {Sku}.",
                model.Sku);

            SetCopyWarning(
                mvcController,
                "Sản phẩm đã được lưu ở thị trường nguồn nhưng chưa thể áp dụng giá cho một số thị trường còn lại.");
        }
    }

    private async Task<int>
        CopyCurrentPriceSchedulesAsync(
            Product product,
            int sourceMarketId,
            IReadOnlyCollection<int> selectedMarketIds,
            DateTime validFrom,
            DateTime? validTo,
            string currentUser)
    {
        var targetMarketIds =
            selectedMarketIds
                .Where(x => x != sourceMarketId)
                .Distinct()
                .ToList();

        if (targetMarketIds.Count == 0)
            return 0;

        var activeTargetMarketIds =
            await _db.Markets
                .AsNoTracking()
                .Where(x =>
                    targetMarketIds.Contains(x.Id) &&
                    x.IsActive)
                .Select(x => x.Id)
                .ToListAsync();

        if (activeTargetMarketIds.Count == 0)
            return 0;

        var activeVariantIds =
            product.HasVariants
                ? await _db.ProductVariants
                    .IgnoreQueryFilters()
                    .AsNoTracking()
                    .Where(x =>
                        x.ProductId == product.Id &&
                        x.IsActive &&
                        !x.IsDeleted)
                    .Select(x => x.Id)
                    .ToListAsync()
                : new List<int>();

        var sourceQuery =
            _db.PriceSchedules
                .AsNoTracking()
                .Where(x =>
                    x.IsActive &&
                    x.MarketId == sourceMarketId &&
                    (
                        x.ProductId == product.Id ||
                        (
                            x.ProductVariantId.HasValue &&
                            activeVariantIds.Contains(
                                x.ProductVariantId.Value)
                        )
                    ));

        var sourceSchedules =
            await sourceQuery
                .Where(x =>
                    x.ValidFrom == validFrom &&
                    x.ValidTo == validTo)
                .ToListAsync();

        if (sourceSchedules.Count == 0)
        {
            var end =
                validTo ?? DateTime.MaxValue;

            sourceSchedules =
                await sourceQuery
                    .Where(x =>
                        x.ValidFrom < end &&
                        validFrom <
                        (x.ValidTo ??
                         DateTime.MaxValue))
                    .OrderByDescending(x =>
                        x.ValidFrom)
                    .ToListAsync();
        }

        if (sourceSchedules.Count == 0)
            return 0;

        await using var transaction =
            await _db.Database
                .BeginTransactionAsync();

        var copiedCount = 0;

        foreach (var targetMarketId in
                 activeTargetMarketIds)
        {
            foreach (var source in
                     sourceSchedules)
            {
                var target =
                    await FindTargetScheduleAsync(
                        source,
                        targetMarketId);

                if (target is null)
                {
                    target = new PriceSchedule
                    {
                        ProductId =
                            source.ProductId,
                        ProductVariantId =
                            source.ProductVariantId,
                        MarketId =
                            targetMarketId,
                        CreatedBy =
                            currentUser
                    };

                    _db.PriceSchedules.Add(
                        target);
                }

                target.CostPrice =
                    source.CostPrice;

                target.ListPrice =
                    source.ListPrice;

                target.SalePrice =
                    source.SalePrice;

                target.ValidFrom =
                    source.ValidFrom;

                target.ValidTo =
                    source.ValidTo;

                target.Note =
                    source.Note;

                target.IsActive = true;
                target.UpdatedBy =
                    currentUser;

                copiedCount++;
            }
        }

        await _db.SaveChangesAsync();
        await transaction.CommitAsync();

        return copiedCount;
    }

    private async Task<PriceSchedule?>
        FindTargetScheduleAsync(
            PriceSchedule source,
            int targetMarketId)
    {
        var end =
            source.ValidTo ??
            DateTime.MaxValue;

        return await _db.PriceSchedules
            .Where(x =>
                x.IsActive &&
                x.MarketId == targetMarketId &&
                x.ProductId ==
                source.ProductId &&
                x.ProductVariantId ==
                source.ProductVariantId &&
                x.ValidFrom < end &&
                source.ValidFrom <
                (x.ValidTo ??
                 DateTime.MaxValue))
            .OrderByDescending(x =>
                x.ValidFrom)
            .FirstOrDefaultAsync();
    }

    private static void SetCopyWarning(
        Controller controller,
        string message)
    {
        controller.TempData["Error"] =
            message;
    }
}
