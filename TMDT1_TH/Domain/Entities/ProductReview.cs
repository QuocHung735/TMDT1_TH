using TMDT1_TH.Domain.Common;
using TMDT1_TH.Domain.Enums;
using TMDT1_TH.Domain.Identity;

namespace TMDT1_TH.Domain.Entities;

public sealed class ProductReview : AuditableEntity
{
    public int ProductId { get; set; }
    public int? ProductVariantId { get; set; }
    public int OrderItemId { get; set; }
    public int CustomerUserId { get; set; }

    public int Rating { get; set; }
    public string? Title { get; set; }
    public string Comment { get; set; } = string.Empty;
    public string CustomerDisplayName { get; set; } = string.Empty;

    public ProductReviewStatus Status { get; set; } =
        ProductReviewStatus.Pending;

    public string? AdminReply { get; set; }
    public DateTime? ModeratedAt { get; set; }
    public DateTime? AdminRepliedAt { get; set; }

    public Product Product { get; set; } = null!;
    public ProductVariant? ProductVariant { get; set; }
    public OrderItem OrderItem { get; set; } = null!;
    public ApplicationUser CustomerUser { get; set; } = null!;
}
