using Ecommerce.Application.DTOs.Orders;

namespace Ecommerce.Application.Interfaces;

public interface IOrderService
{
    Task<OrderResponse> CreateOrderFromCartAsync(Guid userId);

    Task<IEnumerable<OrderResponse>> GetUserOrdersAsync(Guid userId);

    Task<OrderResponse> GetOrderAsync(Guid orderId, Guid userId);

    // NEW: User cancels their own order
    Task<OrderResponse> CancelOrderAsync(
        Guid orderId,
        Guid userId,
        CancelOrderRequest request);

    // NEW: Admin cancels any user's order
    Task<OrderResponse> AdminCancelOrderAsync(
        Guid orderId,
        Guid adminId,
        CancelOrderRequest request);
}