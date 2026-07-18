using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TMDT1_TH.Domain.Entities;

namespace TMDT1_TH.Data.Configurations;

public class ProductOptionValueConfiguration : IEntityTypeConfiguration<ProductOptionValue>
{
    public void Configure(EntityTypeBuilder<ProductOptionValue> builder)
    {
        builder.ToTable("ProductOptionValues");
        builder.ConfigureAudit();

        builder.Property(x => x.Value).HasMaxLength(120).IsRequired();
        builder.Property(x => x.ColorCode).HasMaxLength(20);
        builder.HasIndex(x => new { x.ProductOptionId, x.Value }).IsUnique();

        builder.HasOne(x => x.ProductOption)
            .WithMany(x => x.Values)
            .HasForeignKey(x => x.ProductOptionId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
