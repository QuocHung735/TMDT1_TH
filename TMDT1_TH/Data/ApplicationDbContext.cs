using Microsoft.EntityFrameworkCore;
using TMDT1_TH.Domain.Common;
using TMDT1_TH.Domain.Entities;

namespace TMDT1_TH.Data;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : DbContext(options)
{
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Brand> Brands => Set<Brand>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<ProductImage> ProductImages => Set<ProductImage>();
    public DbSet<ProductOption> ProductOptions => Set<ProductOption>();
    public DbSet<ProductOptionValue> ProductOptionValues => Set<ProductOptionValue>();
    public DbSet<ProductVariant> ProductVariants => Set<ProductVariant>();
    public DbSet<ProductVariantValue> ProductVariantValues => Set<ProductVariantValue>();
    public DbSet<Market> Markets => Set<Market>();
    public DbSet<PriceSchedule> PriceSchedules => Set<PriceSchedule>();
    public DbSet<PriceHistory> PriceHistories => Set<PriceHistory>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
    }

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        ApplyAuditInformation();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    public override Task<int> SaveChangesAsync(
        bool acceptAllChangesOnSuccess,
        CancellationToken cancellationToken = default)
    {
        ApplyAuditInformation();
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    private void ApplyAuditInformation()
    {
        var utcNow = DateTime.UtcNow;

        foreach (var entry in ChangeTracker.Entries<AuditableEntity>())
        {
            if (entry.State == EntityState.Added)
            {
                if (entry.Entity.CreatedAt == default)
                    entry.Entity.CreatedAt = utcNow;

                entry.Entity.CreatedBy ??= "System";
            }
            else if (entry.State == EntityState.Modified)
            {
                entry.Entity.UpdatedAt = utcNow;
                entry.Entity.UpdatedBy ??= "System";
                entry.Property(x => x.CreatedAt).IsModified = false;
                entry.Property(x => x.CreatedBy).IsModified = false;
            }
        }
    }
}
