using System.ComponentModel.DataAnnotations;

namespace SmartStock.Models;

public class Product
{
    public int Id { get; set; }

    [Required, StringLength(100)]
    public string Name { get; set; } = "";

    [Required, StringLength(30)]
    public string SKU { get; set; } = "";

    [Range(0, 1000000)]
    public decimal UnitPrice { get; set; }

    // ConcurrencyCheck: EF Core adds "WHERE Quantity = <value we read>" to the UPDATE,
    // so two simultaneous stock operations cannot silently overwrite each other.
    [Range(0, 1000000), ConcurrencyCheck]
    public int Quantity { get; set; }

    [Range(0, 1000000)]
    public int ReorderLevel { get; set; } = 5;

    public int? CategoryId { get; set; }
    public Category? Category { get; set; }

    public int? SupplierId { get; set; }
    public Supplier? Supplier { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public bool IsLowStock => Quantity <= ReorderLevel;
}
