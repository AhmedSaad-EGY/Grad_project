using Microsoft.Extensions.Logging;
using Sayiad.Data.Common;
using Sayiad.Domain.Dtos.OrderDtos;

namespace Sayiad.Domain.Managers;

public class OrderManager : IOrderManager
{
    private readonly IOrderRepository _orderRepo;
    private readonly ICartRepository _cartRepo;
    private readonly ISellerProfileRepository _sellerProfileRepo;
    private readonly INotificationManager _notificationManager;
    private readonly ILogger<OrderManager> _logger;

    public OrderManager(
        IOrderRepository orderRepo,
        ICartRepository cartRepo,
        ISellerProfileRepository sellerProfileRepo,
        INotificationManager notificationManager,
        ILogger<OrderManager> logger)
    {
        _orderRepo = orderRepo;
        _cartRepo = cartRepo;
        _sellerProfileRepo = sellerProfileRepo;
        _notificationManager = notificationManager;
        _logger = logger;
    }

    public async Task<OrderResponse> CreateFromCartAsync(int userId, CreateOrderRequest request)
    {
        var cart = await _cartRepo.GetCartAsync(userId)
            ?? throw new InvalidOperationException("Cart is empty");

        if (cart.CartItems.Count == 0)
            throw new InvalidOperationException("Cart is empty");

        _ = await GetShippingAddressAsync(userId, request.ShippingAddressId);

        foreach (var item in cart.CartItems)
        {
            if (item.Product.StockQuantity < item.Quantity)
                throw new InvalidOperationException(
                    $"Insufficient stock for {item.Product.Title}");
        }

        var order = new CustomerOrder
        {
            BuyerId = userId,
            ShippingAddressId = request.ShippingAddressId,
            Status = CustomerOrderStatus.Pending,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        foreach (var cartItem in cart.CartItems)
        {
            var subtotal = cartItem.Product.Price * cartItem.Quantity;
            order.TotalPrice += subtotal;

            order.OrderItems.Add(new OrderItem
            {
                ProductId = cartItem.ProductId,
                SellerId = cartItem.Product.SellerId,
                Quantity = cartItem.Quantity,
                UnitPrice = cartItem.Product.Price,
                Subtotal = subtotal,
                CreatedAt = DateTime.UtcNow
            });
        }

        order = await _orderRepo.CreateOrderTransactionAsync(order, userId);

        await _notificationManager.CreateAsync(userId, "Order Placed",
            $"Your order #{order.Id} has been placed successfully.");

        _logger.LogInformation("Order created: {OrderId} by user {UserId}", order.Id, userId);
        return await GetByIdAsync(order.Id, userId);
    }

    public async Task<PagedResult<OrderResponse>> GetUserOrdersAsync(int userId, PaginationRequest? pagination = null)
    {
        var p = pagination ?? new PaginationRequest();
        var result = await _orderRepo.GetUserOrdersAsync(userId, p);
        return new PagedResult<OrderResponse>
        {
            Items = result.Items.Select(MapToResponse).ToList(),
            TotalCount = result.TotalCount,
            Page = result.Page,
            PageSize = result.PageSize
        };
    }

    public async Task<IEnumerable<OrderResponse>> GetSellerOrdersAsync(int sellerId)
    {
        var orders = await _orderRepo.GetSellerOrdersAsync(sellerId);
        return orders.Select(MapToResponse);
    }

    public async Task<OrderResponse> GetByIdAsync(int orderId, int userId)
    {
        var order = await _orderRepo.GetByIdAsync(orderId)
            ?? throw new KeyNotFoundException("Order not found");

        if (order.BuyerId != userId &&
            !order.OrderItems.Any(oi => oi.SellerId == userId))
            throw new UnauthorizedAccessException("Access denied");

        return MapToResponse(order);
    }

    public async Task<OrderResponse> UpdateStatusAsync(int orderId, CustomerOrderStatus status)
    {
        var order = await _orderRepo.GetByIdAsync(orderId)
            ?? throw new KeyNotFoundException("Order not found");

        order.Status = status;
        order.UpdatedAt = DateTime.UtcNow;
        await _orderRepo.UpdateAsync(order);

        if (status == CustomerOrderStatus.Delivered)
        {
            foreach (var sellerId in order.OrderItems.Select(oi => oi.SellerId).Distinct())
                await _sellerProfileRepo.IncrementSalesAsync(sellerId);
        }

        await _notificationManager.CreateAsync(order.BuyerId, "Order Updated",
            $"Your order #{order.Id} status changed to {status}.");

        _logger.LogInformation("Order {OrderId} status updated to {Status}", orderId, status);
        return MapToResponse(order);
    }

    private async Task<ShippingAddress> GetShippingAddressAsync(int userId, int addressId)
    {
        return await _orderRepo.GetShippingAddressAsync(addressId, userId)
            ?? throw new KeyNotFoundException("Shipping address not found");
    }

    private static OrderResponse MapToResponse(CustomerOrder order)
    {
        var items = order.OrderItems.Select(oi => new OrderItemResponse(
            oi.Id, oi.ProductId, oi.Product.Title,
            oi.Product.Images.FirstOrDefault(i => i.IsPrimary)?.ImageUrl,
            oi.SellerId, oi.Seller.FullName,
            oi.Quantity, oi.UnitPrice, oi.Subtotal
        )).ToList();

        return new OrderResponse(
            order.Id, order.BuyerId, order.Buyer.FullName,
            order.TotalPrice, order.Status,
            order.CreatedAt, order.UpdatedAt, items);
    }
}
