using Microsoft.EntityFrameworkCore;
using SmartStock.Models;
using SmartStock.Services;

namespace SmartStock.Data;

public static class DatabaseInitializer
{
    public static async Task InitializeAsync(IDbContextFactory<SmartStockDbContext> factory)
    {
        await using var db = await factory.CreateDbContextAsync();
        await db.Database.EnsureCreatedAsync();

        // Demo users (passwords are stored only as hashes).
        if (!await db.Users.AnyAsync())
        {
            db.Users.AddRange(
                new AppUser { UserName = "admin", PasswordHash = AuthService.HashPassword("Admin123!"), Role = Roles.Admin },
                new AppUser { UserName = "operator", PasswordHash = AuthService.HashPassword("Operator123!"), Role = Roles.Operator });
            await db.SaveChangesAsync();
        }

        if (await db.Categories.AnyAsync()) return;

        var office = new Category { Name = "Office equipment", Description = "Equipment for daily office work" };
        var accessories = new Category { Name = "Accessories", Description = "Computer and desk accessories" };
        var electronics = new Category { Name = "Electronics", Description = "Electronic devices" };
        db.Categories.AddRange(office, accessories, electronics);

        var supplier = new Supplier
        {
            Name = "Northstar Supplies",
            Email = "sales@northstar.example",
            Phone = "+1 555 010 2040",
            Address = "Business district"
        };
        db.Suppliers.Add(supplier);
        await db.SaveChangesAsync();

        db.Products.AddRange(
            new Product { Name = "Wireless Mouse", SKU = "ACC-001", UnitPrice = 24.99m, Quantity = 42, ReorderLevel = 10, Category = accessories, Supplier = supplier },
            new Product { Name = "Mechanical Keyboard", SKU = "ACC-002", UnitPrice = 79.50m, Quantity = 18, ReorderLevel = 8, Category = accessories, Supplier = supplier },
            new Product { Name = "Webcam HD", SKU = "VID-001", UnitPrice = 59.00m, Quantity = 4, ReorderLevel = 6, Category = electronics, Supplier = supplier },
            new Product { Name = "USB-C Hub", SKU = "ACC-003", UnitPrice = 39.99m, Quantity = 5, ReorderLevel = 10, Category = accessories, Supplier = supplier },
            new Product { Name = "Desk Lamp", SKU = "OFF-001", UnitPrice = 31.25m, Quantity = 26, ReorderLevel = 5, Category = office, Supplier = supplier },
            new Product { Name = "Monitor Stand", SKU = "OFF-002", UnitPrice = 44.00m, Quantity = 0, ReorderLevel = 4, Category = office, Supplier = supplier }
        );
        await db.SaveChangesAsync();

        var products = await db.Products.ToListAsync();
        db.Transactions.AddRange(
            new StockTransaction { ProductId = products[0].Id, Type = TransactionType.StockIn, Quantity = 42, Note = "Initial stock" },
            new StockTransaction { ProductId = products[2].Id, Type = TransactionType.StockIn, Quantity = 8, Note = "Initial stock" },
            new StockTransaction { ProductId = products[2].Id, Type = TransactionType.StockOut, Quantity = 4, Note = "Demo issue" }
        );
        await db.SaveChangesAsync();
    }
}
