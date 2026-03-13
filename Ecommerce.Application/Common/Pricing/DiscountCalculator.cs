using Ecommerce.Domain.Entities;
using Ecommerce.Domain.Enums;

namespace Ecommerce.Application.Common.Pricing;

public static class DiscountCalculator
{
    public static decimal CalculateFinalPrice(
        decimal originalPrice,
        Discount? discount)
    {
        if (discount == null || !discount.IsValid())
            return originalPrice;

        decimal finalPrice = discount.DiscountType switch
        {
            DiscountType.Percentage =>
                originalPrice - (originalPrice * discount.Value / 100),

            DiscountType.Fixed =>
                originalPrice - discount.Value,

            _ => originalPrice
        };

        return finalPrice < 0 ? 0 : finalPrice;
    }
}