namespace Ecommerce.Application.DTOs.Payments;

public record InitializePaymentRequest(
    Guid OrderId
);