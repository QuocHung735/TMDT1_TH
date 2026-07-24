using System.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TMDT1_TH.Data;
using TMDT1_TH.Domain.Entities;
using TMDT1_TH.Domain.Enums;
using TMDT1_TH.Infrastructure.Cart;
using TMDT1_TH.ViewModels.Storefront;

namespace TMDT1_TH.Controllers;

[Route("thanh-toan")]
public sealed class CheckoutController(
    ApplicationDbContext db,
    CartSessionStore cartStore,
    ILogger<CheckoutController> logger) : Controller
{
    private readonly ApplicationDbContext _db = db;
    private readonly CartSessionStore _cartStore = cartStore;
    private readonly ILogger<CheckoutController> _logger = logger;

    [HttpGet("")]
    public async Task<IActionResult> Index()
    {
        var cartItems = _cartStore.GetItems();
        if (cartItems.Count == 0)
        {
            TempData["CartMessage"] = "Giỏ hàng đang trống.";
            return RedirectToAction("Index", "Cart");
        }

        var resolution = await ResolveCheckoutAsync(cartItems);
        if (resolution.Errors.Count > 0)
        {
            TempData["CartMessage"] = string.Join(" ", resolution.Errors);
            _cartStore.Save(resolution.ValidSessionItems);
            return RedirectToAction("Index", "Cart");
        }

        return View(BuildCheckoutViewModel(new StoreCheckoutViewModel(), resolution));
    }

    [HttpPost("")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> PlaceOrder(StoreCheckoutViewModel model)
    {
        NormalizeCustomerInput(model);

        // Chạy lại validation sau khi đã Trim để chuỗi chỉ gồm khoảng trắng
        // không thể vượt qua kiểm tra Required.
        ModelState.Clear();
        TryValidateModel(model);

        var sessionItems = _cartStore.GetItems();
        if (sessionItems.Count == 0)
        {
            ModelState.AddModelError(string.Empty, "Giỏ hàng đang trống.");
            return View("Index", model);
        }

        if (!ModelState.IsValid)
        {
            var invalidResolution = await ResolveCheckoutAsync(sessionItems);
            return View(
                "Index",
                BuildCheckoutViewModel(model, invalidResolution));
        }

        try
        {
            await using var transaction =
                await _db.Database.BeginTransactionAsync(
                    IsolationLevel.Serializable);

            var resolution = await ResolveCheckoutAsync(sessionItems);

            foreach (var error in resolution.Errors)
                ModelState.AddModelError(string.Empty, error);

            if (!ModelState.IsValid)
            {
                await transaction.RollbackAsync();
                return View(
                    "Index",
                    BuildCheckoutViewModel(model, resolution));
            }

            foreach (var line in resolution.Items)
            {
                var affected = line.ProductVariantId.HasValue
                    ? await DeductVariantStockAsync(line)
                    : await DeductSimpleProductStockAsync(line);

                if (affected != 1)
                {
                    throw new InvalidOperationException(
                        $"Tồn kho của SKU {line.Sku} vừa thay đổi. Vui lòng kiểm tra lại giỏ hàng.");
                }
            }

            await UpdateParentProductStatusesAsync(resolution.Items);

            var userAgent = Request.Headers.UserAgent.ToString();

            var order = new Order
            {
                OrderNumber = await CreateOrderNumberAsync(),
                PublicToken = Guid.NewGuid(),
                MarketId = resolution.MarketId,
                CurrencyCode = resolution.CurrencyCode,
                Status = OrderStatus.Pending,
                PaymentMethod = PaymentMethod.CashOnDelivery,
                PaymentStatus = PaymentStatus.Unpaid,
                CustomerName = model.CustomerName,
                CustomerPhone = model.CustomerPhone,
                CustomerEmail = NullIfWhiteSpace(model.CustomerEmail),
                Province = model.Province,
                District = model.District,
                Ward = model.Ward,
                AddressLine = model.AddressLine,
                CustomerNote = NullIfWhiteSpace(model.CustomerNote),
                Subtotal = resolution.Subtotal,
                ShippingFee = 0,
                DiscountAmount = 0,
                TotalAmount = resolution.Subtotal,
                CustomerIp = HttpContext.Connection.RemoteIpAddress?.ToString(),
                UserAgent = string.IsNullOrWhiteSpace(userAgent)
                    ? null
                    : userAgent[..Math.Min(userAgent.Length, 500)],
                CreatedBy = "Storefront"
            };

            foreach (var line in resolution.Items)
            {
                order.Items.Add(new OrderItem
                {
                    ProductId = line.ProductId,
                    ProductVariantId = line.ProductVariantId,
                    ProductName = line.ProductName,
                    VariantName = line.VariantName,
                    Sku = line.Sku,
                    ImageUrl = line.ImageUrl,
                    Unit = line.Unit,
                    ListPrice = line.ListPrice,
                    UnitPrice = line.UnitPrice,
                    Quantity = line.Quantity,
                    LineTotal = line.LineTotal,
                    CreatedBy = "Storefront"
                });
            }

            _db.Orders.Add(order);
            await _db.SaveChangesAsync();
            await transaction.CommitAsync();

            _cartStore.Clear();

            return RedirectToAction(
                nameof(Confirmation),
                new
                {
                    orderNumber = order.OrderNumber,
                    publicToken = order.PublicToken
                });
        }
        catch (Exception exception)
        {
            _db.ChangeTracker.Clear();

            _logger.LogError(
                exception,
                "Không thể tạo đơn hàng từ storefront.");

            ModelState.AddModelError(
                string.Empty,
                exception is InvalidOperationException
                    ? exception.Message
                    : "Không thể tạo đơn hàng lúc này. Vui lòng thử lại.");

            var refreshResolution = await ResolveCheckoutAsync(
                _cartStore.GetItems());

            return View(
                "Index",
                BuildCheckoutViewModel(model, refreshResolution));
        }
    }

    [HttpGet("/don-hang/{orderNumber}/{publicToken:guid}")]
    public async Task<IActionResult> Confirmation(
        string orderNumber,
        Guid publicToken)
    {
        var order = await _db.Orders
            .AsNoTracking()
            .Include(x => x.Items)
            .FirstOrDefaultAsync(x =>
                x.OrderNumber == orderNumber &&
                x.PublicToken == publicToken);

        if (order is null)
            return NotFound();

        var model = new StoreOrderConfirmationViewModel
        {
            OrderNumber = order.OrderNumber,
            CreatedAt = order.CreatedAt,
            StatusName = GetOrderStatusName(order.Status),
            PaymentMethodName = order.PaymentMethod ==
                PaymentMethod.CashOnDelivery
                    ? "Thanh toán khi nhận hàng"
                    : order.PaymentMethod.ToString(),
            CustomerName = order.CustomerName,
            CustomerPhone = order.CustomerPhone,
            CustomerEmail = order.CustomerEmail,
            ShippingAddress = string.Join(
                ", ",
                new[]
                {
                    order.AddressLine,
                    order.Ward,
                    order.District,
                    order.Province
                }.Where(x => !string.IsNullOrWhiteSpace(x))),
            CustomerNote = order.CustomerNote,
            CurrencyCode = order.CurrencyCode,
            Subtotal = order.Subtotal,
            ShippingFee = order.ShippingFee,
            TotalAmount = order.TotalAmount,
            Items = order.Items
                .OrderBy(x => x.Id)
                .Select(x => new StoreOrderConfirmationItemViewModel
                {
                    ProductName = x.ProductName,
                    VariantName = x.VariantName,
                    Sku = x.Sku,
                    ImageUrl = x.ImageUrl,
                    UnitPrice = x.UnitPrice,
                    Quantity = x.Quantity,
                    LineTotal = x.LineTotal
                })
                .ToList()
        };

        return View(model);
    }

    private async Task<CheckoutResolution> ResolveCheckoutAsync(
        IReadOnlyList<CartSessionItem> sessionItems)
    {
        var errors = new List<string>();
        var validSessionItems = new List<CartSessionItem>();
        var resolvedItems = new List<CheckoutLine>();

        var market = await _db.Markets
            .AsNoTracking()
            .Where(x => x.IsActive)
            .OrderByDescending(x => x.IsDefault)
            .ThenBy(x => x.Id)
            .Select(x => new
            {
                x.Id,
                x.CurrencyCode
            })
            .FirstOrDefaultAsync();

        if (market is null)
        {
            errors.Add("Chưa có thị trường đang hoạt động để xác định giá.");
            return new CheckoutResolution(
                0,
                "VND",
                resolvedItems,
                validSessionItems,
                errors);
        }

        var productIds = sessionItems
            .Select(x => x.ProductId)
            .Distinct()
            .ToList();

        var products = await _db.Products
            .AsNoTracking()
            .Where(x =>
                productIds.Contains(x.Id) &&
                !x.IsDeleted &&
                x.Status == ProductStatus.Active)
            .Include(x => x.Images)
            .Include(x => x.PriceSchedules)
            .Include(x => x.Variants)
                .ThenInclude(x => x.Images)
            .Include(x => x.Variants)
                .ThenInclude(x => x.PriceSchedules)
            .AsSplitQuery()
            .ToDictionaryAsync(x => x.Id);

        foreach (var sessionItem in sessionItems)
        {
            if (!products.TryGetValue(
                    sessionItem.ProductId,
                    out var product))
            {
                errors.Add(
                    "Một sản phẩm trong giỏ không còn được bán.");
                continue;
            }

            var resolution = ResolveLine(
                product,
                sessionItem,
                market.Id,
                market.CurrencyCode,
                DateTime.UtcNow);

            if (resolution.Line is null)
            {
                errors.Add(
                    resolution.Error ??
                    $"SKU trong sản phẩm {product.Name} không còn hợp lệ.");
                continue;
            }

            resolvedItems.Add(resolution.Line);
            validSessionItems.Add(sessionItem);
        }

        return new CheckoutResolution(
            market.Id,
            market.CurrencyCode,
            resolvedItems,
            validSessionItems,
            errors);
    }

    private static LineResolution ResolveLine(
        Product product,
        CartSessionItem sessionItem,
        int marketId,
        string currencyCode,
        DateTime now)
    {
        ProductVariant? variant = null;
        PriceSchedule? price;
        int stockQuantity;
        string sku;
        string? variantName;
        string? imageUrl;
        decimal? weight;

        if (product.HasVariants)
        {
            if (!sessionItem.ProductVariantId.HasValue)
            {
                return LineResolution.Failed(
                    $"Sản phẩm {product.Name} cần chọn phân loại.");
            }

            variant = product.Variants.FirstOrDefault(x =>
                x.Id == sessionItem.ProductVariantId.Value &&
                x.IsActive &&
                !x.IsDeleted);

            if (variant is null)
            {
                return LineResolution.Failed(
                    $"Phân loại của {product.Name} không còn được bán.");
            }

            price = GetCurrentPrice(
                variant.PriceSchedules,
                marketId,
                now);
            stockQuantity = variant.StockQuantity;
            sku = variant.Sku;
            variantName = variant.Name;
            imageUrl = variant.Images
                .OrderByDescending(x => x.IsPrimary)
                .ThenBy(x => x.DisplayOrder)
                .Select(x => x.ImageUrl)
                .FirstOrDefault();
            weight = variant.Weight ?? product.Weight;
        }
        else
        {
            if (sessionItem.ProductVariantId.HasValue)
            {
                return LineResolution.Failed(
                    $"Sản phẩm {product.Name} không sử dụng phân loại.");
            }

            price = GetCurrentPrice(
                product.PriceSchedules,
                marketId,
                now);
            stockQuantity = product.StockQuantity;
            sku = product.Sku;
            variantName = null;
            imageUrl = null;
            weight = product.Weight;
        }

        if (price is null || price.SalePrice <= 0)
        {
            return LineResolution.Failed(
                $"SKU {sku} chưa có giá bán đang áp dụng.");
        }

        var minQuantity = Math.Max(
            product.MinPurchaseQuantity,
            1);
        var maxByPolicy =
            product.MaxPurchaseQuantity ?? stockQuantity;
        var maxQuantity = Math.Min(
            stockQuantity,
            maxByPolicy);

        if (sessionItem.Quantity < minQuantity ||
            sessionItem.Quantity > maxQuantity)
        {
            return LineResolution.Failed(
                $"Số lượng của SKU {sku} phải từ {minQuantity} đến {maxQuantity}.");
        }

        imageUrl ??= product.Images
            .Where(x => x.ProductVariantId == null)
            .OrderByDescending(x => x.IsPrimary)
            .ThenBy(x => x.DisplayOrder)
            .Select(x => x.ImageUrl)
            .FirstOrDefault();

        return LineResolution.Success(new CheckoutLine(
            product.Id,
            variant?.Id,
            product.Name,
            variantName,
            sku,
            imageUrl,
            product.Unit,
            currencyCode,
            price.ListPrice,
            price.SalePrice,
            sessionItem.Quantity,
            stockQuantity,
            weight));
    }

    private async Task<int> DeductSimpleProductStockAsync(
        CheckoutLine line)
    {
        return await _db.Products
            .IgnoreQueryFilters()
            .Where(x =>
                x.Id == line.ProductId &&
                !x.IsDeleted &&
                x.Status == ProductStatus.Active &&
                !x.HasVariants &&
                x.StockQuantity >= line.Quantity)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(
                    x => x.StockQuantity,
                    x => x.StockQuantity - line.Quantity)
                .SetProperty(
                    x => x.UpdatedAt,
                    DateTime.UtcNow)
                .SetProperty(
                    x => x.UpdatedBy,
                    "Storefront"));
    }

    private async Task<int> DeductVariantStockAsync(
        CheckoutLine line)
    {
        return await _db.ProductVariants
            .IgnoreQueryFilters()
            .Where(x =>
                x.Id == line.ProductVariantId!.Value &&
                x.ProductId == line.ProductId &&
                !x.IsDeleted &&
                x.IsActive &&
                x.StockQuantity >= line.Quantity)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(
                    x => x.StockQuantity,
                    x => x.StockQuantity - line.Quantity)
                .SetProperty(
                    x => x.UpdatedAt,
                    DateTime.UtcNow)
                .SetProperty(
                    x => x.UpdatedBy,
                    "Storefront"));
    }

    private async Task UpdateParentProductStatusesAsync(
        IReadOnlyList<CheckoutLine> lines)
    {
        foreach (var productId in lines
            .Select(x => x.ProductId)
            .Distinct())
        {
            var hasVariants = lines
                .Any(x =>
                    x.ProductId == productId &&
                    x.ProductVariantId.HasValue);

            int remainingStock;

            if (hasVariants)
            {
                remainingStock = await _db.ProductVariants
                    .IgnoreQueryFilters()
                    .Where(x =>
                        x.ProductId == productId &&
                        x.IsActive &&
                        !x.IsDeleted)
                    .SumAsync(x => x.StockQuantity);
            }
            else
            {
                remainingStock = await _db.Products
                    .IgnoreQueryFilters()
                    .Where(x => x.Id == productId)
                    .Select(x => x.StockQuantity)
                    .SingleAsync();
            }

            if (remainingStock == 0)
            {
                await _db.Products
                    .IgnoreQueryFilters()
                    .Where(x =>
                        x.Id == productId &&
                        x.Status == ProductStatus.Active)
                    .ExecuteUpdateAsync(setters => setters
                        .SetProperty(
                            x => x.Status,
                            ProductStatus.OutOfStock)
                        .SetProperty(
                            x => x.UpdatedAt,
                            DateTime.UtcNow)
                        .SetProperty(
                            x => x.UpdatedBy,
                            "Storefront"));
            }
        }
    }

    private async Task<string> CreateOrderNumberAsync()
    {
        for (var attempt = 0; attempt < 10; attempt++)
        {
            var randomPart = Guid.NewGuid()
                .ToString("N")[..8]
                .ToUpperInvariant();

            var candidate =
                $"MH-{DateTime.UtcNow:yyMMdd}-{randomPart}";

            var exists = await _db.Orders
                .AsNoTracking()
                .AnyAsync(x =>
                    x.OrderNumber == candidate);

            if (!exists)
                return candidate;
        }

        throw new InvalidOperationException(
            "Không thể tạo mã đơn hàng duy nhất. Vui lòng thử lại.");
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
                (!x.ValidTo.HasValue ||
                 x.ValidTo.Value > now))
            .OrderByDescending(x => x.ValidFrom)
            .ThenByDescending(x => x.Id)
            .FirstOrDefault();
    }

    private static StoreCheckoutViewModel BuildCheckoutViewModel(
        StoreCheckoutViewModel model,
        CheckoutResolution resolution)
    {
        model.CurrencyCode = resolution.CurrencyCode;
        model.Items = resolution.Items
            .Select(x => new StoreCheckoutItemViewModel
            {
                ProductId = x.ProductId,
                ProductVariantId = x.ProductVariantId,
                ProductName = x.ProductName,
                VariantName = x.VariantName,
                Sku = x.Sku,
                ImageUrl = x.ImageUrl,
                Unit = x.Unit,
                ListPrice = x.ListPrice,
                UnitPrice = x.UnitPrice,
                Quantity = x.Quantity,
                StockQuantity = x.StockQuantity
            })
            .ToList();
        model.TotalQuantity =
            resolution.Items.Sum(x => x.Quantity);
        model.Subtotal =
            resolution.Items.Sum(x => x.LineTotal);
        model.ShippingFee = 0;
        model.TotalAmount = model.Subtotal;

        return model;
    }

    private static void NormalizeCustomerInput(
        StoreCheckoutViewModel model)
    {
        model.CustomerName = model.CustomerName.Trim();
        model.CustomerPhone = model.CustomerPhone.Trim();
        model.CustomerEmail =
            NullIfWhiteSpace(model.CustomerEmail);
        model.Province = model.Province.Trim();
        model.District = model.District.Trim();
        model.Ward = model.Ward.Trim();
        model.AddressLine = model.AddressLine.Trim();
        model.CustomerNote =
            NullIfWhiteSpace(model.CustomerNote);
    }

    private static string? NullIfWhiteSpace(
        string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim();
    }

    private static string GetOrderStatusName(
        OrderStatus status)
    {
        return status switch
        {
            OrderStatus.Pending => "Chờ xác nhận",
            OrderStatus.Confirmed => "Đã xác nhận",
            OrderStatus.Processing => "Đang chuẩn bị hàng",
            OrderStatus.Shipping => "Đang giao hàng",
            OrderStatus.Completed => "Hoàn thành",
            OrderStatus.Cancelled => "Đã hủy",
            _ => status.ToString()
        };
    }

    private sealed record CheckoutLine(
        int ProductId,
        int? ProductVariantId,
        string ProductName,
        string? VariantName,
        string Sku,
        string? ImageUrl,
        string Unit,
        string CurrencyCode,
        decimal ListPrice,
        decimal UnitPrice,
        int Quantity,
        int StockQuantity,
        decimal? Weight)
    {
        public decimal LineTotal =>
            UnitPrice * Quantity;
    }

    private sealed record CheckoutResolution(
        int MarketId,
        string CurrencyCode,
        IReadOnlyList<CheckoutLine> Items,
        IReadOnlyList<CartSessionItem> ValidSessionItems,
        IReadOnlyList<string> Errors)
    {
        public decimal Subtotal =>
            Items.Sum(x => x.LineTotal);
    }

    private sealed record LineResolution(
        CheckoutLine? Line,
        string? Error)
    {
        public static LineResolution Success(
            CheckoutLine line) =>
            new(line, null);

        public static LineResolution Failed(
            string error) =>
            new(null, error);
    }
}
