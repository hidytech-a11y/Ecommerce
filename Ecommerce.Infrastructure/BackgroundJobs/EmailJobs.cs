using Ecommerce.Application.Common.Interfaces;
using Microsoft.Extensions.Logging;

namespace Ecommerce.Infrastructure.BackgroundJobs;

public class EmailJobs
{
    private readonly IEmailService _emailService;
    private readonly ILogger<EmailJobs> _logger;

    public EmailJobs(
        IEmailService emailService,
        ILogger<EmailJobs> logger)
    {
        _emailService = emailService;
        _logger = logger;
    }

    public async Task SendOrderConfirmation(
        string email,
        Guid orderId)
    {
        _logger.LogInformation(
            "Sending order confirmation email for {OrderId}",
            orderId);

        await _emailService.SendAsync(
            email,
            "Order Confirmation",
            $"Your order {orderId} was successful.");
    }
}