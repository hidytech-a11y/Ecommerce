namespace Ecommerce.Application.DTOs.Orders;

public record OrderResponse(
    Guid OrderId,
    decimal TotalAmount,
    string Status,
    DateTime CreatedAt
);