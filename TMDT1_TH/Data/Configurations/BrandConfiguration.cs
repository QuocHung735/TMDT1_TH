using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TMDT1_TH.Domain.Entities;

namespace TMDT1_TH.Data.Configurations;

public class BrandConfiguration : IEntityTypeConfiguration<Brand>
{
    public void Configure(EntityTypeBuilder<Brand> builder)
    {
        builder.ToTable("Brands");
        builder.ConfigureAudit();

        builder.Property(x => x.Name).HasMaxLength(150).IsRequired();
        builder.Property(x => x.Slug).HasMaxLength(180).IsRequired();
        builder.Property(x => x.LogoUrl).HasMaxLength(500);
        builder.Property(x => x.Description).HasMaxLength(1500);
        builder.Property(x => x.WebsiteUrl).HasMaxLength(300);
        builder.Property(x => x.Country).HasMaxLength(100);

        builder.HasIndex(x => x.Slug)
            .IsUnique()
            .HasFilter("[IsDeleted] = 0");
        builder.HasQueryFilter(x => !x.IsDeleted);
    }
}
