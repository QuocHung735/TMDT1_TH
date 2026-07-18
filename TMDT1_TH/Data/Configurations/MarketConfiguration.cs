using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TMDT1_TH.Domain.Entities;

namespace TMDT1_TH.Data.Configurations;

public class MarketConfiguration : IEntityTypeConfiguration<Market>
{
    private static readonly DateTime SeedDate = new(2026, 7, 18, 0, 0, 0, DateTimeKind.Utc);

    public void Configure(EntityTypeBuilder<Market> builder)
    {
        builder.ToTable("Markets");
        builder.ConfigureAudit();

        builder.Property(x => x.Code).HasMaxLength(30).IsRequired();
        builder.Property(x => x.Name).HasMaxLength(150).IsRequired();
        builder.Property(x => x.CurrencyCode).HasMaxLength(10).IsRequired();
        builder.Property(x => x.CountryCode).HasMaxLength(10);
        builder.Property(x => x.Description).HasMaxLength(500);
        builder.HasIndex(x => x.Code).IsUnique();
        builder.HasIndex(x => x.IsDefault)
            .IsUnique()
            .HasFilter("[IsDefault] = 1");

        builder.HasData(
            new Market
            {
                Id = 1,
                Code = "ONLINE",
                Name = "Kênh trực tuyến",
                CurrencyCode = "VND",
                CountryCode = "VN",
                Description = "Giá áp dụng cho website và kênh bán trực tuyến.",
                IsDefault = true,
                IsActive = true,
                CreatedAt = SeedDate,
                CreatedBy = "Seed"
            },
            new Market
            {
                Id = 2,
                Code = "VN-HCM",
                Name = "Thành phố Hồ Chí Minh",
                CurrencyCode = "VND",
                CountryCode = "VN",
                IsDefault = false,
                IsActive = true,
                CreatedAt = SeedDate,
                CreatedBy = "Seed"
            },
            new Market
            {
                Id = 3,
                Code = "VN-HN",
                Name = "Hà Nội",
                CurrencyCode = "VND",
                CountryCode = "VN",
                IsDefault = false,
                IsActive = true,
                CreatedAt = SeedDate,
                CreatedBy = "Seed"
            });
    }
}
