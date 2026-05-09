using Sayiad.Domain.Dtos.CartDtos;

namespace Sayiad.Domain.Contracts;

public interface ICartManager
{
    Task<CartResponse> GetCartAsync(int userId);
    Task<CartResponse> AddItemAsync(int userId, AddToCartRequest request);
    Task<CartResponse> UpdateItemQuantityAsync(int userId, int productId, UpdateCartItemRequest request);
    Task RemoveItemAsync(int userId, int productId);
    Task ClearCartAsync(int userId);
}
