namespace Ecommerce.Application.Interfaces;

public record PaystackInitializeResponse(
    string AuthorizationUrl
);

public record PaystackVerifyResponse(
    bool Success,
    decimal Amount
);