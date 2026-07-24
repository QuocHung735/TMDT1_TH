using System.ComponentModel.DataAnnotations;
using TMDT1_TH.Domain.Enums;

namespace TMDT1_TH.ViewModels.Storefront;

public sealed class CustomerReviewsPageViewModel
{
    public IReadOnlyList<CustomerReviewListItemViewModel> Items { get; init; }
        = Array.Empty<CustomerReviewListItemViewModel>();

    public int PendingCount { get; init; }
    public int ApprovedCount { get; init; }
    public int ReviewableCount { get; init; }
}

public sealed class CustomerReviewListItemViewModel
{
    public int OrderItemId { get; init; }
    public string OrderNumber { get; init; } = string.Empty;
    public DateTime CompletedAt { get; init; }
    public string ProductName { get; init; } = string.Empty;
    public string? VariantName { get; init; }
    public string? ProductSlug { get; init; }
    public string? ImageUrl { get; init; }

    public bool CanReview { get; init; }
    public int? Rating { get; init; }
    public ProductReviewStatus? Status { get; init; }
    public string? StatusName { get; init; }
    public string? StatusClass { get; init; }
    public DateTime? ReviewedAt { get; init; }
}

public sealed class CreateProductReviewViewModel
{
    [Required]
    public int OrderItemId { get; set; }

    public string OrderNumber { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public string? VariantName { get; set; }
    public string? ImageUrl { get; set; }

    [Range(
        1,
        5,
        ErrorMessage = "Vui lòng chọn từ 1 đến 5 sao.")]
    [Display(Name = "Số sao")]
    public int Rating { get; set; } = 5;

    [StringLength(
        150,
        ErrorMessage = "Tiêu đề không được vượt quá 150 ký tự.")]
    [Display(Name = "Tiêu đề")]
    public string? Title { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập nội dung đánh giá.")]
    [StringLength(
        2000,
        MinimumLength = 20,
        ErrorMessage = "Nội dung đánh giá cần từ 20 đến 2000 ký tự.")]
    [Display(Name = "Nội dung đánh giá")]
    public string Comment { get; set; } = string.Empty;
}

public sealed class StoreProductReviewsViewModel
{
    public int ReviewCount { get; init; }
    public decimal AverageRating { get; init; }
    public IReadOnlyList<StoreProductReviewItemViewModel> Items { get; init; }
        = Array.Empty<StoreProductReviewItemViewModel>();
}

public sealed class StoreProductReviewItemViewModel
{
    public int Rating { get; init; }
    public string? Title { get; init; }
    public string Comment { get; init; } = string.Empty;
    public string CustomerDisplayName { get; init; } = string.Empty;
    public string? VariantName { get; init; }
    public string? AdminReply { get; init; }
    public DateTime CreatedAt { get; init; }
}

public static class ProductReviewDisplay
{
    public static string StatusName(ProductReviewStatus status) =>
        status switch
        {
            ProductReviewStatus.Pending => "Chờ duyệt",
            ProductReviewStatus.Approved => "Đã duyệt",
            ProductReviewStatus.Rejected => "Từ chối",
            ProductReviewStatus.Hidden => "Đã ẩn",
            _ => status.ToString()
        };

    public static string StatusClass(ProductReviewStatus status) =>
        status switch
        {
            ProductReviewStatus.Pending => "is-pending",
            ProductReviewStatus.Approved => "is-approved",
            ProductReviewStatus.Rejected => "is-rejected",
            ProductReviewStatus.Hidden => "is-hidden",
            _ => string.Empty
        };
}
