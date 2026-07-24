using TMDT1_TH.Domain.Common;

namespace TMDT1_TH.Domain.Entities;

public sealed class ShippingService : AuditableEntity
{
    public int ShippingCarrierId { get; set; }

    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }

    public decimal BaseFee { get; set; }
    public int EstimatedMinDays { get; set; } = 1;
    public int EstimatedMaxDays { get; set; } = 3;

    public bool IsActive { get; set; } = true;
    public int DisplayOrder { get; set; }

    public ShippingCarrier ShippingCarrier { get; set; } = null!;
    public ICollection<Order> Orders { get; set; }
        = new List<Order>();
}
