using Microsoft.EntityFrameworkCore;
using SmartStock.Data;

namespace SmartStock.Services;

public record DashboardStats(int ProductCount, int UnitsInStock, int LowStockCount, decimal StockValue, int SupplierCount);

public class DashboardService(IDbContextFactory<SmartStockDbContext> factory)
{
    public async Task<DashboardStats> GetStatsAsync()
    {
        await using var db = await factory.CreateDbContextAsync();
        var products = await db.Products.AsNoTracking().ToListAsync();
        return new DashboardStats(
            products.Count,
            products.Sum(p => p.Quantity),
            products.Count(p => p.Quantity <= p.ReorderLevel),
            products.Sum(p => p.UnitPrice * p.Quantity),
            await db.Suppliers.CountAsync());
    }
}
