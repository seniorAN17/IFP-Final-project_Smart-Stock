using SmartStock.Models;
using SmartStock.Services;

namespace SmartStock.Tests;

public class AuthServiceTests : IDisposable
{
    private readonly TestDbFactory _factory = new();
    private readonly AuthService _auth;

    public AuthServiceTests()
    {
        _auth = new AuthService(_factory);
        using var db = _factory.CreateDbContext();
        db.Users.Add(new AppUser { UserName = "admin", PasswordHash = AuthService.HashPassword("Secret123!"), Role = Roles.Admin });
        db.SaveChanges();
    }

    public void Dispose() => _factory.Dispose();

    [Fact]
    public async Task Validate_CorrectCredentials_ReturnsUserWithRole()
    {
        var user = await _auth.ValidateAsync("admin", "Secret123!");

        Assert.NotNull(user);
        Assert.Equal(Roles.Admin, user!.Role);
    }

    [Fact]
    public async Task Validate_UserNameIsTrimmed()
    {
        Assert.NotNull(await _auth.ValidateAsync("  admin  ", "Secret123!"));
    }

    [Fact]
    public async Task Validate_WrongPassword_ReturnsNull()
    {
        Assert.Null(await _auth.ValidateAsync("admin", "wrong"));
    }

    [Fact]
    public async Task Validate_UnknownUser_ReturnsNull()
    {
        Assert.Null(await _auth.ValidateAsync("nobody", "Secret123!"));
    }

    [Fact]
    public void HashPassword_DoesNotStorePlainText()
    {
        var hash = AuthService.HashPassword("Secret123!");

        Assert.DoesNotContain("Secret123!", hash);
    }
}
