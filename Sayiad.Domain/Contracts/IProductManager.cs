using Sayiad.Domain.Contracts;
using Sayiad.Domain.Dtos.ProductDtos;

namespace Sayiad.Domain.Contracts;

public interface IProductManager
{
    Task<IEnumerable<ProductResponse>> GetAllAsync(ProductFilterRequest? filter = null);
    Task<ProductResponse> GetByIdAsync(int id);
    Task<ProductResponse> CreateAsync(int sellerId, CreateProductRequest request);
    Task<ProductResponse> UpdateAsync(int id, int sellerId, UpdateProductRequest request);
    Task DeleteAsync(int id, int sellerId);
    Task<IEnumerable<ProductResponse>> GetSellerProductsAsync(int sellerId);
}
