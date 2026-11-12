using Microsoft.EntityFrameworkCore;
using SmartStock.Models;
using SmartStock.Services;

namespace SmartStock.Tests;

public class InventoryServiceTests : IDisposable
{
    private readonly TestDbFactory _factory = new();
    private readonly InventoryService _service;

    public InventoryServiceTests() => _service = new InventoryService(_factory);

    public void Dispose() => _factory.Dispose();

    // ---------- helpers ----------

    private async Task<Product> AddProductAsync(string name = "Mouse", string sku = "SKU-1", int quantity = 10, int reorder = 5)
    {
        var product = new Product { Name = name, SKU = sku, UnitPrice = 10m, Quantity = quantity, ReorderLevel = reorder };
        await _service.SaveProductAsync(product);
        return product;
    }

    private async Task<Product> ReloadAsync(int id)
    {
        await using var db = _factory.CreateDbContext();
        return await db.Products.AsNoTracking().FirstAsync(p => p.Id == id);
    }

    // ---------- products ----------

    [Fact]
    public async Task SaveProduct_NewProduct_IsStored()
    {
        var product = await AddProductAsync();

        Assert.True(product.Id > 0);
        var all = await _service.GetProductsAsync();
        Assert.Single(all);
    }

    [Fact]
    public async Task SaveProduct_DuplicateSku_Throws()
    {
        await AddProductAsync(sku: "DUP-1");

        var second = new Product { Name = "Other", SKU = "DUP-1" };

        await Assert.ThrowsAsync<InvalidOperationException>(() => _service.SaveProductAsync(second));
    }

    [Fact]
    public async Task SaveProduct_TrimsNameAndSku()
    {
        var product = new Product { Name = "  Keyboard  ", SKU = "  KB-1  " };

        await _service.SaveProductAsync(product);

        var stored = await ReloadAsync(product.Id);
        Assert.Equal("Keyboard", stored.Name);
        Assert.Equal("KB-1", stored.SKU);
    }

    [Fact]
    public async Task SaveProduct_Edit_UpdatesFieldsButKeepsQuantityAndCreatedAt()
    {
        var original = await AddProductAsync(quantity: 10);
        var createdAt = (await ReloadAsync(original.Id)).CreatedAt;

        var edited = new Product
        {
            Id = original.Id,
            Name = "Renamed",
            SKU = "SKU-1",
            UnitPrice = 99m,
            Quantity = 999,            // must be ignored: stock changes only via transactions
            ReorderLevel = 7,
            CreatedAt = DateTime.UtcNow.AddYears(1)   // must be ignored as well
        };
        await _service.SaveProductAsync(edited);

        var stored = await ReloadAsync(original.Id);
        Assert.Equal("Renamed", stored.Name);
        Assert.Equal(99m, stored.UnitPrice);
        Assert.Equal(7, stored.ReorderLevel);
        Assert.Equal(10, stored.Quantity);
        Assert.Equal(createdAt, stored.CreatedAt);
    }

    [Fact]
    public async Task GetProducts_SearchByNameOrSku()
    {
        await AddProductAsync("Wireless Mouse", "ACC-001");
        await AddProductAsync("Desk Lamp", "OFF-001");

        // The database search is case-sensitive in SQLite; the Products page filters case-insensitively in memory.
        var byName = await _service.GetProductsAsync("Mouse");
        var bySku = await _service.GetProductsAsync("OFF-");

        Assert.Single(byName);
        Assert.Equal("Wireless Mouse", byName[0].Name);
        Assert.Single(bySku);
        Assert.Equal("Desk Lamp", bySku[0].Name);
    }

    [Fact]
    public async Task DeleteProduct_WithoutTransactions_RemovesIt()
    {
        var product = await AddProductAsync();

        await _service.DeleteProductAsync(product.Id);

        Assert.Empty(await _service.GetProductsAsync());
    }

    [Fact]
    public async Task DeleteProduct_WithTransactions_Throws()
    {
        var product = await AddProductAsync();
        await _service.RecordTransactionAsync(product.Id, TransactionType.StockIn, 1, "test");

        await Assert.ThrowsAsync<InvalidOperationException>(() => _service.DeleteProductAsync(product.Id));
        Assert.Single(await _service.GetProductsAsync());
    }

    // ---------- stock transactions ----------

    [Fact]
    public async Task RecordTransaction_StockIn_IncreasesQuantityAndStoresHistory()
    {
        var product = await AddProductAsync(quantity: 10);

        await _service.RecordTransactionAsync(product.Id, TransactionType.StockIn, 5, "  delivery  ");

        Assert.Equal(15, (await ReloadAsync(product.Id)).Quantity);
        var history = await _service.GetTransactionsAsync();
        Assert.Single(history);
        Assert.Equal(TransactionType.StockIn, history[0].Type);
        Assert.Equal(5, history[0].Quantity);
        Assert.Equal("delivery", history[0].Note);
    }

    [Fact]
    public async Task RecordTransaction_StockOut_DecreasesQuantity()
    {
        var product = await AddProductAsync(quantity: 10);

        await _service.RecordTransactionAsync(product.Id, TransactionType.StockOut, 4, "sale");

        Assert.Equal(6, (await ReloadAsync(product.Id)).Quantity);
    }

    [Fact]
    public async Task RecordTransaction_StockOutAllUnits_LeavesZero()
    {
        var product = await AddProductAsync(quantity: 3);

        await _service.RecordTransactionAsync(product.Id, TransactionType.StockOut, 3, "all");

        Assert.Equal(0, (await ReloadAsync(product.Id)).Quantity);
    }

    [Fact]
    public async Task RecordTransaction_StockOutMoreThanAvailable_ThrowsAndChangesNothing()
    {
        var product = await AddProductAsync(quantity: 3);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.RecordTransactionAsync(product.Id, TransactionType.StockOut, 4, "too much"));

        Assert.Equal(3, (await ReloadAsync(product.Id)).Quantity);
        Assert.Empty(await _service.GetTransactionsAsync());
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public async Task RecordTransaction_NonPositiveQuantity_Throws(int quantity)
    {
        var product = await AddProductAsync();

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
            () => _service.RecordTransactionAsync(product.Id, TransactionType.StockIn, quantity, ""));
    }

    [Fact]
    public async Task RecordTransaction_UnknownProduct_Throws()
    {
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.RecordTransactionAsync(12345, TransactionType.StockIn, 1, ""));
    }

    // ---------- categories and suppliers ----------

    [Fact]
    public async Task DeleteCategory_WithProducts_Throws()
    {
        var category = new Category { Name = "Accessories" };
        await _service.SaveCategoryAsync(category);
        await _service.SaveProductAsync(new Product { Name = "Mouse", SKU = "A-1", CategoryId = category.Id });

        await Assert.ThrowsAsync<InvalidOperationException>(() => _service.DeleteCategoryAsync(category.Id));
    }

    [Fact]
    public async Task DeleteCategory_WithoutProducts_RemovesIt()
    {
        var category = new Category { Name = "Empty" };
        await _service.SaveCategoryAsync(category);

        await _service.DeleteCategoryAsync(category.Id);

        Assert.Empty(await _service.GetCategoriesAsync());
    }

    [Fact]
    public async Task DeleteSupplier_WithProducts_Throws()
    {
        var supplier = new Supplier { Name = "Northstar" };
        await _service.SaveSupplierAsync(supplier);
        await _service.SaveProductAsync(new Product { Name = "Mouse", SKU = "A-1", SupplierId = supplier.Id });

        await Assert.ThrowsAsync<InvalidOperationException>(() => _service.DeleteSupplierAsync(supplier.Id));
    }
}
