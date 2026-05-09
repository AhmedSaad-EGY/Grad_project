namespace Sayiad.Domain.Contracts;

public interface ICartRepository
{
    Task<Cart?> GetCartAsync(int userId);
    Task AddAsync(Cart cart);
    Task ClearCartAsync(int userId);
    Task SaveChangesAsync();
}
