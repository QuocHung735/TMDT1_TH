using TMDT1_TH.Domain.Common;

namespace TMDT1_TH.Domain.Entities;

public class Brand : AuditableEntity
{
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? LogoUrl { get; set; }
    public string? Description { get; set; }
    public string? WebsiteUrl { get; set; }
    public string? Country { get; set; }
    public bool IsActive { get; set; } = true;
    public bool IsDeleted { get; set; }

    public ICollection<Product> Products { get; set; } = new List<Product>();
}
