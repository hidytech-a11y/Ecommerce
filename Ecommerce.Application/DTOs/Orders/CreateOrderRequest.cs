namespace Ecommerce.Application.DTOs.Orders;

public record CreateOrderRequest(
    IEnumerable<OrderItemRequest> Items
);