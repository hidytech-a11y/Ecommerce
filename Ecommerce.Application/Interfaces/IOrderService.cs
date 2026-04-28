using Ecommerce.Application.DTOs.Orders;

namespace Ecommerce.Application.Interfaces;

public interface IOrderService
{
    //Task<OrderResponse> CreateOrderAsync(Guid userId, CreateOrderRequest request);

    Task<OrderResponse> CreateOrderFromCartAsync(Guid userId);

    Task<IEnumerable<OrderResponse>> GetUserOrdersAsync(Guid userId);

    Task<OrderResponse> GetOrderAsync(Guid orderId, Guid userId);
}