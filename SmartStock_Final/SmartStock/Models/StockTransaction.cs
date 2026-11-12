using System.ComponentModel.DataAnnotations;

namespace SmartStock.Models;

public enum TransactionType { StockIn, StockOut }

public class StockTransaction
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public Product? Product { get; set; }
    public TransactionType Type { get; set; }
    [Range(1, 1000000)]
    public int Quantity { get; set; }
    [StringLength(250)]
    public string Note { get; set; } = "";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
