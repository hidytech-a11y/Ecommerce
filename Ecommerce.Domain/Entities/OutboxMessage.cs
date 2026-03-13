namespace Ecommerce.Domain.Entities;

public class OutboxMessage
{
    public Guid Id { get; private set; } = Guid.NewGuid();

    public string Type { get; private set; }

    public string Payload { get; private set; }

    public DateTime OccurredOnUtc { get; private set; } = DateTime.UtcNow;

    public DateTime? ProcessedOnUtc { get; private set; }

    public OutboxMessage(string type, string payload)
    {
        Type = type;
        Payload = payload;
    }

    public void MarkProcessed()
    {
        ProcessedOnUtc = DateTime.UtcNow;
    }
}