namespace Ecommerce.Application.DTOs.Payments;

public record InitializePaymentResponse(
    string AuthorizationUrl,
    string Reference
);