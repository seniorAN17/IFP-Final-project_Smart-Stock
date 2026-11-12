using Microsoft.EntityFrameworkCore;
using SmartStock.Data;
using SmartStock.Models;

namespace SmartStock.Services;

public class InventoryService(IDbContextFactory<SmartStockDbContext> factory) : IInventoryService
{
    public async Task<List<Product>> GetProductsAsync(string search = "")
    {
        await using var db = await factory.CreateDbContextAsync();
        var query = db.Products.AsNoTracking().Include(p => p.Category).Include(p => p.Supplier).AsQueryable();
        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(p => p.Name.Contains(search) || p.SKU.Contains(search));
        return await query.OrderBy(p => p.Name).ToListAsync();
    }

    public async Task<Product?> GetProductAsync(int id)
    {
        await using var db = await factory.CreateDbContextAsync();
        return await db.Products.AsNoTracking().FirstOrDefaultAsync(p => p.Id == id);
    }

    public async Task SaveProductAsync(Product product)
    {
        product.Name = product.Name.Trim();
        product.SKU = product.SKU.Trim();

        await using var db = await factory.CreateDbContextAsync();
        if (await db.Products.AnyAsync(p => p.SKU == product.SKU && p.Id != product.Id))
            throw new InvalidOperationException("A product with this SKU already exists.");

        if (product.Id == 0)
        {
            db.Products.Add(product);
        }
        else
        {
            // Load the stored product and copy only the editable fields.
            // Quantity and CreatedAt are NOT overwritten: stock changes only through transactions.
            var existing = await db.Products.FindAsync(product.Id)
                ?? throw new InvalidOperationException("Product was not found.");
            existing.Name = product.Name;
            existing.SKU = product.SKU;
            existing.UnitPrice = product.UnitPrice;
            existing.ReorderLevel = product.ReorderLevel;
            existing.CategoryId = product.CategoryId;
            existing.SupplierId = product.SupplierId;
        }
        await db.SaveChangesAsync();
    }

    public async Task DeleteProductAsync(int id)
    {
        await using var db = await factory.CreateDbContextAsync();
        var product = await db.Products.FindAsync(id);
        if (product is null) return;
        if (await db.Transactions.AnyAsync(t => t.ProductId == id))
            throw new InvalidOperationException("This product has transaction history and cannot be deleted.");
        db.Products.Remove(product);
        await db.SaveChangesAsync();
    }

    public async Task<List<Category>> GetCategoriesAsync()
    {
        await using var db = await factory.CreateDbContextAsync();
        return await db.Categories.AsNoTracking().Include(c => c.Products).OrderBy(c => c.Name).ToListAsync();
    }

    public async Task SaveCategoryAsync(Category category)
    {
        await using var db = await factory.CreateDbContextAsync();
        if (category.Id == 0) db.Categories.Add(category); else db.Categories.Update(category);
        await db.SaveChangesAsync();
    }

    public async Task DeleteCategoryAsync(int id)
    {
        await using var db = await factory.CreateDbContextAsync();
        var category = await db.Categories.FindAsync(id);
        if (category is null) return;
        if (await db.Products.AnyAsync(p => p.CategoryId == id))
            throw new InvalidOperationException("Move or remove products in this category before deleting it.");
        db.Categories.Remove(category);
        await db.SaveChangesAsync();
    }

    public async Task<List<Supplier>> GetSuppliersAsync()
    {
        await using var db = await factory.CreateDbContextAsync();
        return await db.Suppliers.AsNoTracking().Include(s => s.Products).OrderBy(s => s.Name).ToListAsync();
    }

    public async Task SaveSupplierAsync(Supplier supplier)
    {
        await using var db = await factory.CreateDbContextAsync();
        if (supplier.Id == 0) db.Suppliers.Add(supplier); else db.Suppliers.Update(supplier);
        await db.SaveChangesAsync();
    }

    public async Task DeleteSupplierAsync(int id)
    {
        await using var db = await factory.CreateDbContextAsync();
        var supplier = await db.Suppliers.FindAsync(id);
        if (supplier is null) return;
        if (await db.Products.AnyAsync(p => p.SupplierId == id))
            throw new InvalidOperationException("Assign the supplier's products to another supplier before deleting it.");
        db.Suppliers.Remove(supplier);
        await db.SaveChangesAsync();
    }

    public async Task<List<StockTransaction>> GetTransactionsAsync()
    {
        await using var db = await factory.CreateDbContextAsync();
        return await db.Transactions.AsNoTracking().Include(t => t.Product).OrderByDescending(t => t.CreatedAt).ToListAsync();
    }

    public async Task RecordTransactionAsync(int productId, TransactionType type, int quantity, string note)
    {
        if (quantity <= 0) throw new ArgumentOutOfRangeException(nameof(quantity), "Quantity must be greater than zero.");
        await using var db = await factory.CreateDbContextAsync();
        var product = await db.Products.FirstOrDefaultAsync(p => p.Id == productId)
            ?? throw new InvalidOperationException("Product was not found.");

        if (type == TransactionType.StockOut && product.Quantity < quantity)
            throw new InvalidOperationException("Not enough stock for this operation.");

        product.Quantity += type == TransactionType.StockIn ? quantity : -quantity;
        db.Transactions.Add(new StockTransaction
        {
            ProductId = productId,
            Type = type,
            Quantity = quantity,
            Note = note?.Trim() ?? ""
        });

        try
        {
            await db.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            // Quantity is a concurrency token: another operation changed the stock after we read it.
            throw new InvalidOperationException("The stock quantity was changed by another operation. Please reload the page and try again.");
        }
    }
}
