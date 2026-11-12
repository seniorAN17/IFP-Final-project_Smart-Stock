using System.ComponentModel.DataAnnotations;

namespace SmartStock.Models;

public class Category
{
    public int Id { get; set; }
    [Required, StringLength(60)]
    public string Name { get; set; } = "";
    [StringLength(250)]
    public string Description { get; set; } = "";
    public List<Product> Products { get; set; } = [];
}
