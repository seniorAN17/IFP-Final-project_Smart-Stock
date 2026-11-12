using System.ComponentModel.DataAnnotations;

namespace SmartStock.Models;

public class Supplier
{
    public int Id { get; set; }
    [Required, StringLength(100)]
    public string Name { get; set; } = "";
    [EmailAddress, StringLength(120)]
    public string Email { get; set; } = "";
    [StringLength(30)]
    public string Phone { get; set; } = "";
    [StringLength(200)]
    public string Address { get; set; } = "";
    public List<Product> Products { get; set; } = [];
}
