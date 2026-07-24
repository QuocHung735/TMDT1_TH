using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TMDT1_TH.Domain.Entities;

namespace TMDT1_TH.Data.Configurations;

public sealed class ShippingCarrierConfiguration
    : IEntityTypeConfiguration<ShippingCarrier>
{
    public void Configure(
        EntityTypeBuilder<ShippingCarrier> builder)
    {
        builder.ToTable("ShippingCarriers");

        builder.ConfigureAudit();

        builder.Property(x => x.Code)
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(x => x.Name)
            .HasMaxLength(150)
            .IsRequired();

        builder.Property(x => x.PhoneNumber)
            .HasMaxLength(30);

        builder.Property(x => x.WebsiteUrl)
            .HasMaxLength(500);

        builder.Property(x => x.TrackingUrlTemplate)
            .HasMaxLength(700);

        builder.HasIndex(x => x.Code)
            .IsUnique();

        builder.HasIndex(x => new
        {
            x.IsActive,
            x.DisplayOrder
        });

        builder.HasData(new ShippingCarrier
        {
            Id = 1,
            Code = "MAYHOME",
            Name = "Mây Home Delivery",
            PhoneNumber = null,
            WebsiteUrl = null,
            TrackingUrlTemplate = null,
            IsActive = true,
            DisplayOrder = 1,
            CreatedAt = new DateTime(
                2026,
                1,
                1,
                0,
                0,
                0,
                DateTimeKind.Utc),
            CreatedBy = "Seed"
        });
    }
}
