using Ecommerce.Application.Common.Exceptions;
using Ecommerce.Application.Common.Utilities;
using Ecommerce.Application.DTOs.Payments;
using Ecommerce.Application.Events;
using Ecommerce.Application.Interfaces;
using Ecommerce.Domain.Entities;
using Ecommerce.Domain.Enums;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace Ecommerce.Application.Services;

public class PaymentService : IPaymentService
{
    private readonly IOrderRepository _orderRepository;
    private readonly IPaystackClient _paystackClient;
    private readonly ILogger<PaymentService> _logger;
    private readonly IOutboxRepository _outboxRepository;
    private readonly IUserRepository _userRepository;

    public PaymentService(
        IOrderRepository orderRepository,
        IPaystackClient paystackClient,
        ILogger<PaymentService> logger,
        IOutboxRepository outboxRepository,
        IUserRepository userRepository)
    {
        _orderRepository = orderRepository;
        _paystackClient = paystackClient;
        _logger = logger;
        _outboxRepository = outboxRepository;
        _userRepository = userRepository;
    }

    
    public async Task<InitializePaymentResponse> InitializePaymentAsync(
        Guid userId,
        InitializePaymentRequest request)
    {
        var order = await _orderRepository.GetByIdAsync(request.OrderId)
                     ?? throw new NotFoundException("Order not found.");

        if (order.UserId != userId)
            throw new UnauthorizedException("Access denied.");

        if (order.Status != OrderStatus.Pending)
            throw new BadRequestException("Order cannot be paid.");

        var reference = PaymentReferenceGenerator.Generate();

        var payment = await _paystackClient.InitializeTransactionAsync(
            order.TotalAmount,
            reference);

        order.SetPaymentReference(reference);

        await _orderRepository.SaveChangesAsync();

        _logger.LogInformation("Payment initialized. OrderId={OrderId}, Ref={Ref}", order.Id, reference);

        return new InitializePaymentResponse(payment.AuthorizationUrl, reference);
    }

    
    public async Task VerifyPaymentAsync(string reference)
    {
        var order = await _orderRepository.GetByPaymentReferenceAsync(reference);

        if (order is null)
            throw new NotFoundException("Order not found.");

        // Idempotency
        if (order.Status == OrderStatus.Paid)
        {
            _logger.LogInformation("Order already paid. Skipping. OrderId={OrderId}", order.Id);
            return;
        }

        var verification = await _paystackClient.VerifyTransactionAsync(reference);

        if (!verification.Success)
            throw new BadRequestException("Payment verification failed.");

        if (verification.Amount != order.TotalAmount)
            throw new BadRequestException("Payment amount mismatch.");

        var user = await _userRepository.GetByIdAsync(order.UserId)
                   ?? throw new NotFoundException("User not found.");

        order.MarkAsPaid(reference);

        // OUTBOX EVENT
        var payload = JsonSerializer.Serialize(new OrderPaidEvent
        {
            OrderId = order.Id,
            Email = user.Email
        });

        await _outboxRepository.AddAsync(new OutboxMessage("OrderPaid", payload));

        await _orderRepository.SaveChangesAsync();
        await _outboxRepository.SaveChangesAsync();

        _logger.LogInformation("Order marked as PAID. OrderId={OrderId}", order.Id);
    }

    
    public async Task HandleWebhookAsync(HttpRequest request)
    {
        request.Body.Position = 0;

        using var reader = new StreamReader(request.Body);
        var body = await reader.ReadToEndAsync();

        using var doc = JsonDocument.Parse(body);

        var eventType = doc.RootElement.GetProperty("event").GetString();

        if (eventType != "charge.success")
        {
            _logger.LogInformation("Ignored event: {Event}", eventType);
            return;
        }

        var reference = doc.RootElement
            .GetProperty("data")
            .GetProperty("reference")
            .GetString();

        if (string.IsNullOrEmpty(reference))
        {
            _logger.LogWarning("Webhook missing reference");
            return;
        }

        _logger.LogInformation("Webhook received. Ref={Ref}", reference);

        await VerifyPaymentAsync(reference);
    }
}