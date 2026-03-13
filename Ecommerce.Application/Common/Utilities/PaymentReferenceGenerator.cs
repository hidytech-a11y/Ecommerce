namespace Ecommerce.Application.Common.Utilities;

public static class PaymentReferenceGenerator
{
    public static string Generate()
    {
        return $"ORD-{Guid.NewGuid():N}";
    }
}