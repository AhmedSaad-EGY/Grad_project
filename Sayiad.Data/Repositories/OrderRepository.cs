using Sayiad.Domain.Contracts;

namespace Sayiad.Data.Repositories;

public class OrderRepository : IOrderRepository
{
    private readonly ApplicationDbContext _db;

    public OrderRepository(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<IEnumerable<CustomerOrder>> GetUserOrdersAsync(int userId)
    {
        return await _db.CustomerOrders
            .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Product)
                    .ThenInclude(p => p.Images)
            .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Seller)
            .Where(o => o.BuyerId == userId)
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync();
    }

    public async Task<IEnumerable<CustomerOrder>> GetSellerOrdersAsync(int sellerId)
    {
        return await _db.CustomerOrders
            .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Product)
                    .ThenInclude(p => p.Images)
            .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Seller)
            .Include(o => o.Buyer)
            .Where(o => o.OrderItems.Any(oi => oi.SellerId == sellerId))
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync();
    }

    public async Task<CustomerOrder?> GetByIdAsync(int orderId)
    {
        return await _db.CustomerOrders
            .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Product)
                    .ThenInclude(p => p.Images)
            .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Seller)
            .Include(o => o.Buyer)
            .FirstOrDefaultAsync(o => o.Id == orderId);
    }

    public async Task AddAsync(CustomerOrder order)
    {
        _db.CustomerOrders.Add(order);
        await _db.SaveChangesAsync();
    }

    public async Task UpdateAsync(CustomerOrder order)
    {
        _db.CustomerOrders.Update(order);
        await _db.SaveChangesAsync();
    }

    public async Task<ShippingAddress?> GetShippingAddressAsync(int addressId, int userId)
    {
        return await _db.ShippingAddresses
            .FirstOrDefaultAsync(a => a.Id == addressId && a.UserId == userId);
    }
}
