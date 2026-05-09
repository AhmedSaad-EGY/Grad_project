using Sayiad.Domain.Enums;

namespace Sayiad.Domain.Contracts;

public interface IProductRepository
{
    Task<IEnumerable<Product>> GetAllAsync(ProductFilterRequest filter);
    Task<Product?> GetByIdAsync(int id);
    Task<IEnumerable<Product>> GetSellerProductsAsync(int sellerId);
    Task AddAsync(Product product);
    Task UpdateAsync(Product product);
    Task<bool> ExistsAsync(int id);
}
