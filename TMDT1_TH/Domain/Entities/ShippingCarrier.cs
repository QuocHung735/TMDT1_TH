using TMDT1_TH.Domain.Common;

namespace TMDT1_TH.Domain.Entities;

public sealed class ShippingCarrier : AuditableEntity
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public string? WebsiteUrl { get; set; }
    public string? TrackingUrlTemplate { get; set; }
    public bool IsActive { get; set; } = true;
    public int DisplayOrder { get; set; }

    public ICollection<ShippingService> Services { get; set; }
        = new List<ShippingService>();
}
