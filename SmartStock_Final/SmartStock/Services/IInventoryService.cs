using SmartStock.Models;

namespace SmartStock.Services;

public interface IInventoryService
{
    Task<List<Product>> GetProductsAsync(string search = "");
    Task<Product?> GetProductAsync(int id);
    Task SaveProductAsync(Product product);
    Task DeleteProductAsync(int id);
    Task<List<Category>> GetCategoriesAsync();
    Task SaveCategoryAsync(Category category);
    Task DeleteCategoryAsync(int id);
    Task<List<Supplier>> GetSuppliersAsync();
    Task SaveSupplierAsync(Supplier supplier);
    Task DeleteSupplierAsync(int id);
    Task<List<StockTransaction>> GetTransactionsAsync();
    Task RecordTransactionAsync(int productId, TransactionType type, int quantity, string note);
}
