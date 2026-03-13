using Ecommerce.Application.Interfaces;
using Ecommerce.Domain.Enums;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

public class PaymentReconciliationService : BackgroundService
{
    private readonly IServiceProvider _services;
    private readonly ILogger<PaymentReconciliationService> _logger;

    public PaymentReconciliationService(IServiceProvider services,
        ILogger<PaymentReconciliationService> logger)
    {
        _services = services;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(
        CancellationToken stoppingToken)
    {
        _logger.LogInformation(
            "Reconciling pending payments...");

        while (!stoppingToken.IsCancellationRequested)
        {
            using var scope = _services.CreateScope();

            var orderRepo = scope.ServiceProvider
                .GetRequiredService<IOrderRepository>();

            var paystack = scope.ServiceProvider
                .GetRequiredService<IPaystackClient>();

            var orders = await orderRepo.GetPendingPaymentOrdersAsync();

            foreach (var order in orders)
            {
                _logger.LogInformation(
                    "Reconciled order {OrderId}",
                    order.Id);

                var verification =
                    await paystack.VerifyTransactionAsync(
                        order.PaymentReference);

                if (verification.Success &&
                    verification.Amount == order.TotalAmount)
                {
                    order.MarkAsPaid(order.PaymentReference);
                }
            }

            await orderRepo.SaveChangesAsync();

            await Task.Delay(
                TimeSpan.FromMinutes(5),
                stoppingToken);
        }
    }
}