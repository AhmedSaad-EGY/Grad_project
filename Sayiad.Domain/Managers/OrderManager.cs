using System.Transactions;
using Microsoft.Extensions.Logging;
using Sayiad.Domain.Contracts;
using Sayiad.Domain.Enums;
using Sayiad.Domain.Dtos.OrderDtos;

namespace Sayiad.Domain.Managers;

public class OrderManager : IOrderManager
{
    private readonly IOrderRepository _orderRepo;
    private readonly ICartRepository _cartRepo;
    private readonly IProductRepository _productRepo;
    private readonly ILogger<OrderManager> _logger;

    public OrderManager(
        IOrderRepository orderRepo,
        ICartRepository cartRepo,
        IProductRepository productRepo,
        ILogger<OrderManager> logger)
    {
        _orderRepo = orderRepo;
        _cartRepo = cartRepo;
        _productRepo = productRepo;
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
            Status = CustomerOrderStatus.Pending,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        using var tx = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled);

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

            cartItem.Product.StockQuantity -= cartItem.Quantity;
            if (cartItem.Product.StockQuantity == 0)
                cartItem.Product.Status = ProductStatus.Sold;

            await _productRepo.UpdateAsync(cartItem.Product);
        }

        await _orderRepo.AddAsync(order);
        await _cartRepo.ClearCartAsync(userId);

        tx.Complete();

        _logger.LogInformation("Order created: {OrderId} by user {UserId}", order.Id, userId);
        return await GetByIdAsync(order.Id, userId);
    }

    public async Task<IEnumerable<OrderResponse>> GetUserOrdersAsync(int userId)
    {
        var orders = await _orderRepo.GetUserOrdersAsync(userId);
        return orders.Select(MapToResponse);
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