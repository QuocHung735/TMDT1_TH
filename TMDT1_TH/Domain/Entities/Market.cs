using TMDT1_TH.Domain.Common;

namespace TMDT1_TH.Domain.Entities;

public class Market : AuditableEntity
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string CurrencyCode { get; set; } = "VND";
    public string? CountryCode { get; set; } = "VN";
    public string? Description { get; set; }
    public bool IsDefault { get; set; }
    public bool IsActive { get; set; } = true;

    public ICollection<PriceSchedule> PriceSchedules { get; set; } = new List<PriceSchedule>();
}
