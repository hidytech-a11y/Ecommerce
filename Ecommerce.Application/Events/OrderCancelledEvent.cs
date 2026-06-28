namespace Ecommerce.Application.Events;

public class OrderCancelledEvent
{
    public Guid OrderId { get; set; }
    public Guid UserId { get; set; }
    public string Email { get; set; } = default!;
    public string? Reason { get; set; }
    public DateTime CancelledAt { get; set; }
}