using Microsoft.AspNetCore.Mvc.Rendering;
using TMDT1_TH.Domain.Enums;

namespace TMDT1_TH.Areas.Admin.ViewModels;

public sealed class AdminReviewsViewModel
{
    public string? Query { get; init; }
    public ProductReviewStatus? Status { get; init; }
    public int? Rating { get; init; }

    public int TotalCount { get; init; }
    public int PendingCount { get; init; }
    public int ApprovedCount { get; init; }
    public int RejectedCount { get; init; }
    public int HiddenCount { get; init; }

    public IReadOnlyList<AdminReviewListItemViewModel> Items { get; init; }
        = Array.Empty<AdminReviewListItemViewModel>();
}

public sealed class AdminReviewListItemViewModel
{
    public int Id { get; init; }
    public string ProductName { get; init; } = string.Empty;
    public string? VariantName { get; init; }
    public string CustomerName { get; init; } = string.Empty;
    public string OrderNumber { get; init; } = string.Empty;
    public int Rating { get; init; }
    public string CommentPreview { get; init; } = string.Empty;
    public ProductReviewStatus Status { get; init; }
    public string StatusName { get; init; } = string.Empty;
    public string StatusClass { get; init; } = string.Empty;
    public DateTime CreatedAt { get; init; }
}

public sealed class AdminReviewDetailsViewModel
{
    public int Id { get; init; }
    public string ProductName { get; init; } = string.Empty;
    public string? ProductSlug { get; init; }
    public string? VariantName { get; init; }
    public string? ImageUrl { get; init; }

    public string CustomerName { get; init; } = string.Empty;
    public string? CustomerEmail { get; init; }
    public string OrderNumber { get; init; } = string.Empty;

    public int Rating { get; init; }
    public string? Title { get; init; }
    public string Comment { get; init; } = string.Empty;
    public DateTime CreatedAt { get; init; }

    public ProductReviewStatus Status { get; init; }
    public string StatusName { get; init; } = string.Empty;
    public string StatusClass { get; init; } = string.Empty;
    public string? AdminReply { get; init; }
    public DateTime? ModeratedAt { get; init; }

    public IReadOnlyList<SelectListItem> StatusOptions { get; init; }
        = Array.Empty<SelectListItem>();
}
