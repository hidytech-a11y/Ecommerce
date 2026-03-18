using Ecommerce.Application.Events;
using Ecommerce.Infrastructure.BackgroundJobs;
using Ecommerce.Infrastructure.Persistence;
using Hangfire;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Text.Json;

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
                        "Processing outbox message {MessageId} of type {Type}",
                        message.Id,
                        message.Type);

                    // Handle OrderPaid event
                    if (message.Type == "OrderPaid")
                    {
                        var payload = JsonSerializer
                            .Deserialize<OrderPaidEvent>(message.Payload);

                        if (payload != null)
                        {
                            BackgroundJob.Enqueue<EmailJobs>(
                                job => job.SendOrderConfirmation(
                                    payload.Email,
                                    payload.OrderId));
                        }
                    }

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