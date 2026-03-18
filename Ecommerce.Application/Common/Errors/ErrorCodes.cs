namespace Ecommerce.Application.Common.Errors;

public static class ErrorCodes
{
    public const string NotFound = "NOT_FOUND";
    public const string Unauthorized = "UNAUTHORIZED";
    public const string ValidationFailed = "VALIDATION_FAILED";
    public const string PaymentFailed = "PAYMENT_FAILED";
    public const string InventoryConflict = "INVENTORY_CONFLICT";

    // Domain-specific
    public const string ProductNotFound = "PRODUCT_NOT_FOUND";
    public const string OrderNotFound = "ORDER_NOT_FOUND";
}