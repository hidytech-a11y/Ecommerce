using Ecommerce.Domain.Common;

namespace Ecommerce.Domain.Entities;

public class OrderItem : BaseEntity
{
    public Guid ProductId { get; private set; }
    public string ProductNameSnapshot { get; private set; } = default!;
    public decimal OriginalPriceSnapshot { get; private set; }
    public decimal FinalPriceSnapshot { get; private set; }
    public int Quantity { get; private set; }

    private OrderItem() { }

    public OrderItem(
        Guid productId,
        string name,
        decimal originalPrice,
        decimal finalPrice,
        int quantity)
    {
        ProductId = productId;
        ProductNameSnapshot = name;
        OriginalPriceSnapshot = originalPrice;
        FinalPriceSnapshot = finalPrice;
        Quantity = quantity;
    }
}