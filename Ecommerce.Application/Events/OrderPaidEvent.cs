namespace Ecommerce.Application.Events;

public class OrderPaidEvent
{
    public Guid OrderId { get; set; }

    public string Email { get; set; }
}