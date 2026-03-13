using Ecommerce.Domain.Common;
using Ecommerce.Domain.Enums;

namespace Ecommerce.Domain.Entities;

public class Discount : BaseEntity
{
    public Guid ProductId { get; private set; }
    public DiscountType DiscountType { get; private set; }
    public decimal Value { get; private set; }
    public DateTime StartDate { get; private set; }
    public DateTime EndDate { get; private set; }
    public bool IsActive { get; private set; }

    private Discount() { }

    public Discount(
        Guid productId,
        DiscountType type,
        decimal value,
        DateTime start,
        DateTime end)
    {
        ProductId = productId;
        DiscountType = type;
        Value = value;
        StartDate = start;
        EndDate = end;
        IsActive = true;
    }

    public bool IsValid()
    {
        var now = DateTime.UtcNow;
        return IsActive && now >= StartDate && now <= EndDate;
    }
}