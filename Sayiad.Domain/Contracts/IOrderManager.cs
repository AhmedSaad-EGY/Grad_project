using Sayiad.Domain.Enums;
using Sayiad.Domain.Dtos.OrderDtos;

namespace Sayiad.Domain.Contracts;

public interface IOrderManager
{
    Task<OrderResponse> CreateFromCartAsync(int userId, CreateOrderRequest request);
    Task<IEnumerable<OrderResponse>> GetUserOrdersAsync(int userId);
    Task<IEnumerable<OrderResponse>> GetSellerOrdersAsync(int sellerId);
    Task<OrderResponse> GetByIdAsync(int orderId, int userId);
    Task<OrderResponse> UpdateStatusAsync(int orderId, CustomerOrderStatus status);
}
