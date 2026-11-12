using System.ComponentModel.DataAnnotations;

namespace SmartStock.Models;

// Role names used by the application.
public static class Roles
{
    public const string Admin = "Admin";       // can add, edit and delete
    public const string Operator = "Operator"; // can add and edit, but cannot delete
}

public class AppUser
{
    public int Id { get; set; }

    [Required, StringLength(50)]
    public string UserName { get; set; } = "";

    // Only a salted hash is stored, never the real password.
    [Required]
    public string PasswordHash { get; set; } = "";

    [Required, StringLength(20)]
    public string Role { get; set; } = Roles.Operator;
}
