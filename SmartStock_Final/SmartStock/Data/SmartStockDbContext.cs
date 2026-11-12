using Microsoft.EntityFrameworkCore;
using SmartStock.Models;

namespace SmartStock.Data;

public class SmartStockDbContext(DbContextOptions<SmartStockDbContext> options) : DbContext(options)
{
    public DbSet<Product> Products => Set<Product>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Supplier> Suppliers => Set<Supplier>();
    public DbSet<StockTransaction> Transactions => Set<StockTransaction>();
    public DbSet<AppUser> Users => Set<AppUser>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Product>()
            .HasIndex(p => p.SKU).IsUnique();

        modelBuilder.Entity<AppUser>()
            .HasIndex(u => u.UserName).IsUnique();

        modelBuilder.Entity<Product>()
            .Property(p => p.UnitPrice).HasPrecision(12, 2);

        modelBuilder.Entity<Product>()
            .HasOne(p => p.Category).WithMany(c => c.Products)
            .HasForeignKey(p => p.CategoryId).OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<Product>()
            .HasOne(p => p.Supplier).WithMany(s => s.Products)
            .HasForeignKey(p => p.SupplierId).OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<StockTransaction>()
            .HasOne(t => t.Product).WithMany()
            .HasForeignKey(t => t.ProductId).OnDelete(DeleteBehavior.Restrict);
    }
}
