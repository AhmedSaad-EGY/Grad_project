namespace Sayiad.Domain.Contracts;

public interface IWishlistRepository
{
    Task<IEnumerable<Wishlist>> GetUserWishlistAsync(int userId);
    Task<Wishlist?> GetByUserAndProductAsync(int userId, int productId);
    Task AddAsync(Wishlist wishlist);
    Task RemoveAsync(Wishlist wishlist);
}
