namespace Ecommerce.Application.DTOs.Orders;

public record OrderItemRequest(
    Guid ProductId,
    int Quantity
);