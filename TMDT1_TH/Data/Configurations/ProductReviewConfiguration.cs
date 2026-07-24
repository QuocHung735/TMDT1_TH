using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TMDT1_TH.Domain.Entities;

namespace TMDT1_TH.Data.Configurations;

public sealed class ProductReviewConfiguration
    : IEntityTypeConfiguration<ProductReview>
{
    public void Configure(EntityTypeBuilder<ProductReview> builder)
    {
        builder.ToTable("ProductReviews", table =>
        {
            table.HasCheckConstraint(
                "CK_ProductReviews_Rating",
                "[Rating] >= 1 AND [Rating] <= 5");
        });

        builder.ConfigureAudit();

        builder.Property(x => x.Title)
            .HasMaxLength(150);

        builder.Property(x => x.Comment)
            .HasMaxLength(2000)
            .IsRequired();

        builder.Property(x => x.CustomerDisplayName)
            .HasMaxLength(150)
            .IsRequired();

        builder.Property(x => x.Status)
            .HasConversion<int>();

        builder.Property(x => x.AdminReply)
            .HasMaxLength(1000);

        builder.Property(x => x.ModeratedAt)
            .HasColumnType("datetime2");

        builder.Property(x => x.AdminRepliedAt)
            .HasColumnType("datetime2");

        // Một dòng hàng đã mua chỉ được đánh giá một lần.
        builder.HasIndex(x => x.OrderItemId)
            .IsUnique();

        builder.HasIndex(x => new
        {
            x.ProductId,
            x.Status,
            x.CreatedAt
        });

        builder.HasIndex(x => new
        {
            x.CustomerUserId,
            x.CreatedAt
        });

        builder.HasOne(x => x.Product)
            .WithMany()
            .HasForeignKey(x => x.ProductId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasOne(x => x.ProductVariant)
            .WithMany()
            .HasForeignKey(x => x.ProductVariantId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasOne(x => x.OrderItem)
            .WithMany()
            .HasForeignKey(x => x.OrderItemId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasOne(x => x.CustomerUser)
            .WithMany()
            .HasForeignKey(x => x.CustomerUserId)
            .OnDelete(DeleteBehavior.NoAction);
    }
}
