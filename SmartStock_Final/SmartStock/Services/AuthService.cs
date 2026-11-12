using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SmartStock.Data;
using SmartStock.Models;

namespace SmartStock.Services;

/// <summary>Checks a user name and password against the Users table.</summary>
public class AuthService(IDbContextFactory<SmartStockDbContext> factory)
{
    // PasswordHasher comes with ASP.NET Core: it creates salted PBKDF2 hashes.
    private static readonly PasswordHasher<AppUser> Hasher = new();

    public static string HashPassword(string password) => Hasher.HashPassword(new AppUser(), password);

    /// <summary>Returns the user when the credentials are correct, otherwise null.</summary>
    public async Task<AppUser?> ValidateAsync(string userName, string password)
    {
        var name = (userName ?? "").Trim();
        await using var db = await factory.CreateDbContextAsync();
        var user = await db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.UserName == name);
        if (user is null) return null;

        var result = Hasher.VerifyHashedPassword(user, user.PasswordHash, password ?? "");
        return result == PasswordVerificationResult.Failed ? null : user;
    }
}
