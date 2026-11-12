using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using SmartStock.Data;

namespace SmartStock.Tests;

/// <summary>
/// Creates a fresh in-memory SQLite database for every test.
/// The connection stays open, otherwise the in-memory database would disappear.
/// </summary>
public sealed class TestDbFactory : IDbContextFactory<SmartStockDbContext>, IDisposable
{
    private readonly SqliteConnection _connection;

    public TestDbFactory()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();
        using var db = CreateDbContext();
        db.Database.EnsureCreated();
    }

    public SmartStockDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<SmartStockDbContext>()
            .UseSqlite(_connection)
            .Options;
        return new SmartStockDbContext(options);
    }

    public void Dispose() => _connection.Dispose();
}
