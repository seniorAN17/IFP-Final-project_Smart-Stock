using Microsoft.EntityFrameworkCore;
using SmartStock.Data;
using SmartStock.Models;
using SmartStock.Services;

namespace SmartStock.Tests;

public class ProductModelTests
{
    [Theory]
    [InlineData(0, 5, true)]    // out of stock
    [InlineData(5, 5, true)]    // exactly at the reorder level
    [InlineData(6, 5, false)]   // above the reorder level
    public void IsLowStock_FollowsReorderLevel(int quantity, int reorderLevel, bool expected)
    {
        var product = new Product { Quantity = quantity, ReorderLevel = reorderLevel };

        Assert.Equal(expected, product.IsLowStock);
    }
}

public class DashboardServiceTests : IDisposable
{
    private readonly TestDbFactory _factory = new();

    public void Dispose() => _factory.Dispose();

    [Fact]
    public async Task GetStats_CalculatesTotals()
    {
        var inventory = new InventoryService(_factory);
        await inventory.SaveSupplierAsync(new Supplier { Name = "S1" });
        await inventory.SaveProductAsync(new Product { Name = "A", SKU = "A", UnitPrice = 10m, Quantity = 10, ReorderLevel = 5 });  // healthy
        await inventory.SaveProductAsync(new Product { Name = "B", SKU = "B", UnitPrice = 2.5m, Quantity = 4, ReorderLevel = 5 });   // low

        var stats = await new DashboardService(_factory).GetStatsAsync();

        Assert.Equal(2, stats.ProductCount);
        Assert.Equal(14, stats.UnitsInStock);
        Assert.Equal(1, stats.LowStockCount);
        Assert.Equal(110m, stats.StockValue);   // 10*10 + 4*2.5
        Assert.Equal(1, stats.SupplierCount);
    }
}

public class DatabaseInitializerTests : IDisposable
{
    private readonly TestDbFactory _factory = new();

    public void Dispose() => _factory.Dispose();

    [Fact]
    public async Task Initialize_SeedsDemoData_AndIsSafeToRunTwice()
    {
        await DatabaseInitializer.InitializeAsync(_factory);
        await DatabaseInitializer.InitializeAsync(_factory);

        var inventory = new InventoryService(_factory);
        Assert.Equal(6, (await inventory.GetProductsAsync()).Count);
        Assert.Equal(3, (await inventory.GetCategoriesAsync()).Count);
        Assert.Single(await inventory.GetSuppliersAsync());

        await using var db = _factory.CreateDbContext();
        Assert.Equal(2, await db.Users.CountAsync());   // admin + operator, not duplicated
    }
}
