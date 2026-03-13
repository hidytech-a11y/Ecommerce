using Ecommerce.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

public class OutboxProcessor : BackgroundService
{
    private readonly IServiceProvider _services;
    private readonly ILogger<OutboxProcessor> _logger;

    public OutboxProcessor(
        IServiceProvider services,
        ILogger<OutboxProcessor> logger)
    {
        _services = services;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(
        CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            using var scope = _services.CreateScope();

            var db = scope.ServiceProvider
                .GetRequiredService<AppDbContext>();

            var messages = await db.OutboxMessages
                .Where(x => x.ProcessedOnUtc == null)
                .Take(20)
                .ToListAsync(stoppingToken);

            foreach (var message in messages)
            {
                try
                {
                    _logger.LogInformation(
                        "Processing outbox message {MessageId}",
                        message.Id);

                    // publish event (email, notification, etc.)

                    message.MarkProcessed();
                }
                catch (Exception ex)
                {
                    _logger.LogError(
                        ex,
                        "Failed to process outbox message {MessageId}",
                        message.Id);
                }
            }

            await db.SaveChangesAsync(stoppingToken);

            await Task.Delay(
                TimeSpan.FromSeconds(10),
                stoppingToken);
        }
    }
}