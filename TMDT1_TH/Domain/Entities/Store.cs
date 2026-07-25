using TMDT1_TH.Domain.Common;

namespace TMDT1_TH.Domain.Entities;

public sealed class Store : AuditableEntity
{
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? LogoUrl { get; set; }

    public string? ContactEmail { get; set; }
    public string? PhoneNumber { get; set; }

    public string? AddressLine { get; set; }
    public string? Ward { get; set; }
    public string? District { get; set; }
    public string? Province { get; set; }

    public bool IsActive { get; set; } = true;
    public bool IsVerified { get; set; }

    // Chưa có điểm khi cửa hàng chưa phát sinh đủ đơn hoàn thành.
    // Bước sau sẽ tính lại từ lịch sử giao hàng và hủy đơn.
    public decimal? ReliabilityScore { get; set; }

    public int DisplayOrder { get; set; }
    public bool IsDeleted { get; set; }

    public ICollection<Product> Products { get; set; }
        = new List<Product>();
}
