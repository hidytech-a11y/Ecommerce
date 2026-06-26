namespace Ecommerce.Application.DTOs.Orders;

public record AdminOrderResponse(
    Guid Id,
    decimal TotalAmount,
    string Status,
    string? PaymentReference,
    DateTime? PaidAt,
    DateTime CreatedAt,
    AdminOrderCustomer Customer,
    IEnumerable<AdminOrderItemResponse> Items
);

public record AdminOrderCustomer(
    Guid Id,
    string Email,
    string FirstName,
    string LastName
);

public record AdminOrderItemResponse(
    Guid Id,
    Guid ProductId,
    string ProductName,
    decimal OriginalPrice,
    decimal FinalPrice,
    int Quantity,
    decimal Subtotal
);