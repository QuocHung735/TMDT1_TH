using System.Text.Json;
using Microsoft.AspNetCore.Http;

namespace TMDT1_TH.Infrastructure.Cart;

public sealed class CartSessionStore(IHttpContextAccessor httpContextAccessor)
{
    private const string SessionKey = "MAY_HOME_CART_V1";

    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private ISession Session =>
        httpContextAccessor.HttpContext?.Session
        ?? throw new InvalidOperationException("Session chưa sẵn sàng cho request hiện tại.");

    public IReadOnlyList<CartSessionItem> GetItems()
    {
        var json = Session.GetString(SessionKey);
        if (string.IsNullOrWhiteSpace(json))
            return Array.Empty<CartSessionItem>();

        try
        {
            var items = JsonSerializer.Deserialize<List<CartSessionItem>>(
                json,
                SerializerOptions);

            if (items is null)
                return Array.Empty<CartSessionItem>();

            return items
                .Where(x => x.ProductId > 0 && x.Quantity > 0)
                .GroupBy(x => new { x.ProductId, x.ProductVariantId })
                .Select(group => new CartSessionItem
                {
                    ProductId = group.Key.ProductId,
                    ProductVariantId = group.Key.ProductVariantId,
                    Quantity = group.Sum(x => x.Quantity),
                    AddedAtUtc = group.Min(x => x.AddedAtUtc)
                })
                .OrderBy(x => x.AddedAtUtc)
                .ToList();
        }
        catch (JsonException)
        {
            Session.Remove(SessionKey);
            return Array.Empty<CartSessionItem>();
        }
    }

    public void Save(IEnumerable<CartSessionItem> items)
    {
        var normalized = items
            .Where(x => x.ProductId > 0 && x.Quantity > 0)
            .GroupBy(x => new { x.ProductId, x.ProductVariantId })
            .Select(group => new CartSessionItem
            {
                ProductId = group.Key.ProductId,
                ProductVariantId = group.Key.ProductVariantId,
                Quantity = group.Sum(x => x.Quantity),
                AddedAtUtc = group.Min(x => x.AddedAtUtc)
            })
            .OrderBy(x => x.AddedAtUtc)
            .ToList();

        if (normalized.Count == 0)
        {
            Session.Remove(SessionKey);
            return;
        }

        Session.SetString(
            SessionKey,
            JsonSerializer.Serialize(normalized, SerializerOptions));
    }

    public void Clear() => Session.Remove(SessionKey);
}

public sealed class CartSessionItem
{
    public int ProductId { get; init; }
    public int? ProductVariantId { get; init; }
    public int Quantity { get; init; }
    public DateTime AddedAtUtc { get; init; } = DateTime.UtcNow;
}
