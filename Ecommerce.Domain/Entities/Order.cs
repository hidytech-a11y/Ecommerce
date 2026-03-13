using Ecommerce.Domain.Common;
using Ecommerce.Domain.Enums;

namespace Ecommerce.Domain.Entities;

public class Order : BaseEntity
{
    public Guid UserId { get; private set; }
    public decimal TotalAmount { get; private set; }
    public OrderStatus Status { get; private set; }
    public string? PaymentReference { get; private set; }
    public DateTime? PaidAt { get; private set; }

    private readonly List<OrderItem> _items = new();
    public IReadOnlyCollection<OrderItem> Items => _items;



    private Order() { }

    public Order(Guid userId)
    {
        UserId = userId;
        Status = OrderStatus.Pending;
    }

    public void AddItem(OrderItem item)
    {
        _items.Add(item);
        TotalAmount += item.FinalPriceSnapshot * item.Quantity;
    }

    public void MarkAsPaid(string reference)
    {
        if (Status != OrderStatus.Pending)
            throw new InvalidOperationException("Order cannot be paid.");

        Status = OrderStatus.Paid;
        PaymentReference = reference;
        PaidAt = DateTime.UtcNow;
    }

    public void SetPaymentReference(string reference)
    {
        if (!string.IsNullOrWhiteSpace(PaymentReference))
            throw new InvalidOperationException("Payment reference already set.");

        PaymentReference = reference;
    }
}