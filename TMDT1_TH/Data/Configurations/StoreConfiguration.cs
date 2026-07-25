using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TMDT1_TH.Domain.Entities;

namespace TMDT1_TH.Data.Configurations;

public sealed class StoreConfiguration :
    IEntityTypeConfiguration<Store>
{
    public void Configure(
        EntityTypeBuilder<Store> builder)
    {
        builder.ToTable(
            "Stores",
            table =>
            {
                table.HasCheckConstraint(
                    "CK_Stores_ReliabilityScore",
                    "[ReliabilityScore] IS NULL OR " +
                    "([ReliabilityScore] >= 0 AND " +
                    "[ReliabilityScore] <= 100)");

                table.HasCheckConstraint(
                    "CK_Stores_DisplayOrder",
                    "[DisplayOrder] >= 0");
            });

        builder.ConfigureAudit();

        builder.Property(x => x.Name)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(x => x.Slug)
            .HasMaxLength(220)
            .IsRequired();

        builder.Property(x => x.Description)
            .HasMaxLength(1200);

        builder.Property(x => x.LogoUrl)
            .HasMaxLength(500);

        builder.Property(x => x.ContactEmail)
            .HasMaxLength(256);

        builder.Property(x => x.PhoneNumber)
            .HasMaxLength(30);

        builder.Property(x => x.AddressLine)
            .HasMaxLength(400);

        builder.Property(x => x.Ward)
            .HasMaxLength(150);

        builder.Property(x => x.District)
            .HasMaxLength(150);

        builder.Property(x => x.Province)
            .HasMaxLength(150);

        builder.Property(x => x.ReliabilityScore)
            .HasPrecision(5, 2);

        builder.Property(x => x.IsActive)
            .HasDefaultValue(true);

        builder.Property(x => x.IsVerified)
            .HasDefaultValue(false);

        builder.Property(x => x.DisplayOrder)
            .HasDefaultValue(0);

        builder.Property(x => x.IsDeleted)
            .HasDefaultValue(false);

        builder.HasIndex(x => x.Slug)
            .IsUnique()
            .HasFilter("[IsDeleted] = 0");

        builder.HasIndex(x => new
        {
            x.IsActive,
            x.DisplayOrder,
            x.Name
        });

        builder.HasQueryFilter(x => !x.IsDeleted);
    }
}
