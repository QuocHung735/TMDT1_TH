using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TMDT1_TH.Data;
using TMDT1_TH.Domain.Entities;
using TMDT1_TH.Domain.Enums;
using TMDT1_TH.Infrastructure.Cart;
using TMDT1_TH.ViewModels.Storefront;

namespace TMDT1_TH.Controllers;

[Route("gio-hang")]
public sealed class CartController(
    ApplicationDbContext db,
    CartSessionStore cartStore) : Controller
{
    private const int MaxDistinctCartLines = 50;

    private readonly ApplicationDbContext _db = db;
    private readonly CartSessionStore _cartStore = cartStore;

    [HttpGet("")]
    public async Task<IActionResult> Index()
    {
        var model = await BuildCartPageAsync();
        return View(model);
    }

    [HttpGet("tom-tat")]
    public IActionResult Summary()
    {
        var items = _cartStore.GetItems();

        return Json(new
        {
            itemCount = items.Sum(x => x.Quantity),
            lineCount = items.Count
        });
    }

    [HttpPost("them")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Add(StoreCartAddRequest request)
    {
        if (request.ProductId <= 0)
            return BadRequest(new { message = "Sản phẩm không hợp lệ." });

        var resolution = await ResolvePurchasableAsync(
            request.ProductId,
            request.ProductVariantId);

        if (resolution.Item is null)
            return BadRequest(new { message = resolution.Error });

        var item = resolution.Item;

        if (request.Quantity < item.MinQuantity)
        {
            return BadRequest(new
            {
                message = $"Số lượng tối thiểu là {item.MinQuantity}."
            });
        }

        var sessionItems = _cartStore.GetItems().ToList();
        var existing = sessionItems.FirstOrDefault(x =>
            x.ProductId == request.ProductId &&
            x.ProductVariantId == request.ProductVariantId);

        if (existing is null && sessionItems.Count >= MaxDistinctCartLines)
        {
            return BadRequest(new
            {
                message = $"Giỏ hàng chỉ hỗ trợ tối đa {MaxDistinctCartLines} loại sản phẩm."
            });
        }

        var finalQuantity = (existing?.Quantity ?? 0) + request.Quantity;
        if (finalQuantity > item.MaxQuantity)
        {
            return BadRequest(new
            {
                message = $"Chỉ có thể mua tối đa {item.MaxQuantity} sản phẩm cho SKU này."
            });
        }

        if (existing is not null)
        {
            sessionItems.Remove(existing);
        }

        sessionItems.Add(new CartSessionItem
        {
            ProductId = request.ProductId,
            ProductVariantId = request.ProductVariantId,
            Quantity = finalQuantity,
            AddedAtUtc = existing?.AddedAtUtc ?? DateTime.UtcNow
        });

        _cartStore.Save(sessionItems);

        return Json(new
        {
            message = "Đã thêm sản phẩm vào giỏ hàng.",
            itemCount = sessionItems.Sum(x => x.Quantity),
            cartUrl = Url.Action(nameof(Index), "Cart")
        });
    }

    [HttpPost("cap-nhat")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Update(StoreCartUpdateRequest request)
    {
        var sessionItems = _cartStore.GetItems().ToList();
        var existing = sessionItems.FirstOrDefault(x =>
            x.ProductId == request.ProductId &&
            x.ProductVariantId == request.ProductVariantId);

        if (existing is null)
        {
            TempData["CartMessage"] = "Sản phẩm không còn trong giỏ hàng.";
            return RedirectToAction(nameof(Index));
        }

        var resolution = await ResolvePurchasableAsync(
            request.ProductId,
            request.ProductVariantId);

        if (resolution.Item is null)
        {
            sessionItems.Remove(existing);
            _cartStore.Save(sessionItems);
            TempData["CartMessage"] =
                resolution.Error ?? "Sản phẩm không còn đủ điều kiện mua.";
            return RedirectToAction(nameof(Index));
        }

        var item = resolution.Item;

        if (request.Quantity < item.MinQuantity ||
            request.Quantity > item.MaxQuantity)
        {
            TempData["CartMessage"] =
                $"Số lượng hợp lệ cho {item.Sku} là từ {item.MinQuantity} đến {item.MaxQuantity}.";
            return RedirectToAction(nameof(Index));
        }

        sessionItems.Remove(existing);
        sessionItems.Add(new CartSessionItem
        {
            ProductId = existing.ProductId,
            ProductVariantId = existing.ProductVariantId,
            Quantity = request.Quantity,
            AddedAtUtc = existing.AddedAtUtc
        });

        _cartStore.Save(sessionItems);
        TempData["CartMessage"] = "Đã cập nhật số lượng.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("xoa")]
    [ValidateAntiForgeryToken]
    public IActionResult Remove(StoreCartRemoveRequest request)
    {
        var sessionItems = _cartStore.GetItems().ToList();
        var removed = sessionItems.RemoveAll(x =>
            x.ProductId == request.ProductId &&
            x.ProductVariantId == request.ProductVariantId);

        _cartStore.Save(sessionItems);

        TempData["CartMessage"] = removed > 0
            ? "Đã xóa sản phẩm khỏi giỏ hàng."
            : "Sản phẩm không còn trong giỏ hàng.";

        return RedirectToAction(nameof(Index));
    }

    [HttpPost("xoa-tat-ca")]
    [ValidateAntiForgeryToken]
    public IActionResult Clear()
    {
        _cartStore.Clear();
        TempData["CartMessage"] = "Đã xóa toàn bộ giỏ hàng.";
        return RedirectToAction(nameof(Index));
    }

    private async Task<StoreCartPageViewModel> BuildCartPageAsync()
    {
        var sessionItems = _cartStore.GetItems()
            .OrderBy(x => x.AddedAtUtc)
            .ToList();

        var validSessionItems = new List<CartSessionItem>();
        var cartItems = new List<StoreCartItemViewModel>();
        var warnings = new List<string>();
        var changed = false;

        foreach (var sessionItem in sessionItems)
        {
            var resolution = await ResolvePurchasableAsync(
                sessionItem.ProductId,
                sessionItem.ProductVariantId);

            if (resolution.Item is null)
            {
                changed = true;
                warnings.Add(
                    resolution.Error ??
                    "Một sản phẩm không còn đủ điều kiện mua và đã được xóa.");
                continue;
            }

            var item = resolution.Item;
            var quantity = Math.Clamp(
                sessionItem.Quantity,
                item.MinQuantity,
                item.MaxQuantity);

            if (quantity != sessionItem.Quantity)
            {
                changed = true;
                warnings.Add(
                    $"Số lượng của {item.Sku} đã được điều chỉnh về {quantity} theo tồn kho hiện tại.");
            }

            validSessionItems.Add(new CartSessionItem
            {
                ProductId = sessionItem.ProductId,
                ProductVariantId = sessionItem.ProductVariantId,
                Quantity = quantity,
                AddedAtUtc = sessionItem.AddedAtUtc
            });

            cartItems.Add(new StoreCartItemViewModel
            {
                ProductId = item.ProductId,
                ProductVariantId = item.ProductVariantId,
                ProductName = item.ProductName,
                VariantName = item.VariantName,
                ProductSlug = item.ProductSlug,
                Sku = item.Sku,
                ImageUrl = item.ImageUrl,
                CurrencyCode = item.CurrencyCode,
                SalePrice = item.SalePrice,
                ListPrice = item.ListPrice,
                Quantity = quantity,
                MinQuantity = item.MinQuantity,
                MaxQuantity = item.MaxQuantity,
                StockQuantity = item.StockQuantity
            });
        }

        if (changed)
            _cartStore.Save(validSessionItems);

        return new StoreCartPageViewModel
        {
            Items = cartItems,
            Warnings = warnings,
            CurrencyCode = cartItems.FirstOrDefault()?.CurrencyCode ?? "VND",
            TotalQuantity = cartItems.Sum(x => x.Quantity),
            Subtotal = cartItems.Sum(x => x.LineTotal)
        };
    }

    private async Task<PurchaseResolution> ResolvePurchasableAsync(
        int productId,
        int? productVariantId)
    {
        var market = await GetStoreMarketAsync();
        if (market is null)
        {
            return PurchaseResolution.Failed(
                "Chưa có thị trường đang hoạt động để xác định giá.");
        }

        var product = await _db.Products
            .AsNoTracking()
            .Where(x =>
                x.Id == productId &&
                !x.IsDeleted &&
                x.Status == ProductStatus.Active)
            .Include(x => x.Images)
            .Include(x => x.PriceSchedules)
            .Include(x => x.Variants)
                .ThenInclude(x => x.Images)
            .Include(x => x.Variants)
                .ThenInclude(x => x.PriceSchedules)
            .AsSplitQuery()
            .FirstOrDefaultAsync();

        if (product is null)
            return PurchaseResolution.Failed("Sản phẩm không còn được bán.");

        ProductVariant? variant = null;
        PriceSchedule? price;
        int stockQuantity;
        string sku;
        string? variantName;
        string? imageUrl;

        if (product.HasVariants)
        {
            if (!productVariantId.HasValue)
            {
                return PurchaseResolution.Failed(
                    "Vui lòng chọn đầy đủ phân loại sản phẩm.");
            }

            variant = product.Variants.FirstOrDefault(x =>
                x.Id == productVariantId.Value &&
                x.IsActive &&
                !x.IsDeleted);

            if (variant is null)
            {
                return PurchaseResolution.Failed(
                    "Phân loại đã chọn không còn được bán.");
            }

            price = GetCurrentPrice(
                variant.PriceSchedules,
                market.Id,
                DateTime.UtcNow);

            stockQuantity = variant.StockQuantity;
            sku = variant.Sku;
            variantName = variant.Name;
            imageUrl = variant.Images
                .OrderByDescending(x => x.IsPrimary)
                .ThenBy(x => x.DisplayOrder)
                .Select(x => x.ImageUrl)
                .FirstOrDefault();
        }
        else
        {
            if (productVariantId.HasValue)
            {
                return PurchaseResolution.Failed(
                    "Sản phẩm này không sử dụng phân loại.");
            }

            price = GetCurrentPrice(
                product.PriceSchedules,
                market.Id,
                DateTime.UtcNow);

            stockQuantity = product.StockQuantity;
            sku = product.Sku;
            variantName = null;
            imageUrl = null;
        }

        if (price is null || price.SalePrice <= 0)
        {
            return PurchaseResolution.Failed(
                $"SKU {sku} chưa có giá bán đang áp dụng.");
        }

        var minQuantity = Math.Max(product.MinPurchaseQuantity, 1);
        var maxByPolicy = product.MaxPurchaseQuantity ?? stockQuantity;
        var maxQuantity = Math.Min(stockQuantity, maxByPolicy);

        if (stockQuantity <= 0 || maxQuantity < minQuantity)
        {
            return PurchaseResolution.Failed(
                $"SKU {sku} hiện không đủ tồn kho tối thiểu để mua.");
        }

        imageUrl ??= product.Images
            .Where(x => x.ProductVariantId == null)
            .OrderByDescending(x => x.IsPrimary)
            .ThenBy(x => x.DisplayOrder)
            .Select(x => x.ImageUrl)
            .FirstOrDefault();

        return PurchaseResolution.Success(new PurchasableCartItem(
            product.Id,
            variant?.Id,
            product.Name,
            variantName,
            product.Slug,
            sku,
            imageUrl,
            market.CurrencyCode,
            price.SalePrice,
            price.ListPrice,
            stockQuantity,
            minQuantity,
            maxQuantity));
    }

    private async Task<StoreMarketContext?> GetStoreMarketAsync()
    {
        return await _db.Markets
            .AsNoTracking()
            .Where(x => x.IsActive)
            .OrderByDescending(x => x.IsDefault)
            .ThenBy(x => x.Id)
            .Select(x => new StoreMarketContext(
                x.Id,
                x.CurrencyCode))
            .FirstOrDefaultAsync();
    }

    private static PriceSchedule? GetCurrentPrice(
        IEnumerable<PriceSchedule> schedules,
        int marketId,
        DateTime now)
    {
        return schedules
            .Where(x =>
                x.IsActive &&
                x.MarketId == marketId &&
                x.ValidFrom <= now &&
                (!x.ValidTo.HasValue || x.ValidTo.Value > now))
            .OrderByDescending(x => x.ValidFrom)
            .ThenByDescending(x => x.Id)
            .FirstOrDefault();
    }

    private sealed record StoreMarketContext(
        int Id,
        string CurrencyCode);

    private sealed record PurchasableCartItem(
        int ProductId,
        int? ProductVariantId,
        string ProductName,
        string? VariantName,
        string ProductSlug,
        string Sku,
        string? ImageUrl,
        string CurrencyCode,
        decimal SalePrice,
        decimal? ListPrice,
        int StockQuantity,
        int MinQuantity,
        int MaxQuantity);

    private sealed record PurchaseResolution(
        PurchasableCartItem? Item,
        string? Error)
    {
        public static PurchaseResolution Success(PurchasableCartItem item) =>
            new(item, null);

        public static PurchaseResolution Failed(string error) =>
            new(null, error);
    }
}
