using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TMDT1_TH.Domain.Identity;

namespace TMDT1_TH.Data.Configurations;

public sealed class ApplicationUserConfiguration
    : IEntityTypeConfiguration<ApplicationUser>
{
    public void Configure(EntityTypeBuilder<ApplicationUser> builder)
    {
        builder.Property(x => x.FullName)
            .HasMaxLength(150)
            .IsRequired();

        builder.Property(x => x.DateOfBirth)
            .HasColumnType("date");

        builder.Property(x => x.Gender)
            .HasMaxLength(20);

        builder.Property(x => x.Province)
            .HasMaxLength(150);

        builder.Property(x => x.District)
            .HasMaxLength(150);

        builder.Property(x => x.Ward)
            .HasMaxLength(150);

        builder.Property(x => x.AddressLine)
            .HasMaxLength(500);

        builder.Property(x => x.IsActive)
            .HasDefaultValue(true);

        builder.Property(x => x.CreatedAt)
            .HasColumnType("datetime2")
            .HasDefaultValueSql("SYSUTCDATETIME()");

        builder.HasIndex(x => x.CreatedAt);
    }
}
