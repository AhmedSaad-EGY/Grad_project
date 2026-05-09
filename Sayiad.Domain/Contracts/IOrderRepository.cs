using Sayiad.Domain.Enums;

namespace Sayiad.Domain.Contracts;

public interface IOrderRepository
{
    Task<IEnumerable<CustomerOrder>> GetUserOrdersAsync(int userId);
    Task<IEnumerable<CustomerOrder>> GetSellerOrdersAsync(int sellerId);
    Task<CustomerOrder?> GetByIdAsync(int orderId);
    Task AddAsync(CustomerOrder order);
    Task UpdateAsync(CustomerOrder order);
    Task<ShippingAddress?> GetShippingAddressAsync(int addressId, int userId);
}
