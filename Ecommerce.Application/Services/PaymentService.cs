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
        _logger.LogInformation(
            "Payment initialization requested. OrderId={OrderId} UserId={UserId}",
            request.OrderId,
            userId);

        var order = await _orderRepository.GetByIdAsync(request.OrderId);

        if (order is null)
        {
            _logger.LogWarning(
                "Payment initialization failed. Order not found. OrderId={OrderId}",
                request.OrderId);

            throw new NotFoundException("Order not found.");
        }

        if (order.UserId != userId)
        {
            _logger.LogWarning(
                "Unauthorized payment initialization attempt. OrderId={OrderId} OwnerUserId={OwnerUserId} RequestUserId={RequestUserId}",
                order.Id,
                order.UserId,
                userId);

            throw new UnauthorizedException("Access denied.");
        }

        if (order.Status != OrderStatus.Pending)
        {
            _logger.LogWarning(
                "Payment initialization blocked. Order not in pending state. OrderId={OrderId} Status={Status}",
                order.Id,
                order.Status);

            throw new BadRequestException("Order cannot be paid.");
        }

        var reference = PaymentReferenceGenerator.Generate();

        _logger.LogInformation(
            "Generating Paystack transaction. OrderId={OrderId} Reference={Reference} Amount={Amount}",
            order.Id,
            reference,
            order.TotalAmount);

        var payment = await _paystackClient.InitializeTransactionAsync(
            order.TotalAmount,
            reference);

        order.SetPaymentReference(reference);

        await _orderRepository.SaveChangesAsync();

        _logger.LogInformation(
            "Payment initialization successful. OrderId={OrderId} Reference={Reference}",
            order.Id,
            reference);

        return new InitializePaymentResponse(
            payment.AuthorizationUrl,
            reference);
    }

    public async Task HandleWebhookAsync(HttpRequest request)
    {
        _logger.LogInformation("Paystack webhook received.");

        request.Body.Position = 0;

        using var reader = new StreamReader(request.Body);
        var body = await reader.ReadToEndAsync();

        using var doc = JsonDocument.Parse(body);

        var eventType = doc.RootElement.GetProperty("event").GetString();

        _logger.LogInformation(
            "Webhook event received from Paystack. EventType={EventType}",
            eventType);

        if (eventType != "charge.success")
        {
            _logger.LogInformation(
                "Webhook ignored. Unsupported event type received. EventType={EventType}",
                eventType);

            return;
        }

        var data = doc.RootElement.GetProperty("data");
        var reference = data.GetProperty("reference").GetString();

        _logger.LogInformation(
            "Processing payment webhook. Reference={Reference}",
            reference);

        var order = await _orderRepository.GetByPaymentReferenceAsync(reference);

        if (order is null)
        {
            _logger.LogWarning(
                "Webhook processing failed. Order not found for payment reference. Reference={Reference}",
                reference);

            throw new NotFoundException("Order not found.");
        }

        // Idempotency protection
        if (order.Status == OrderStatus.Paid)
        {
            _logger.LogInformation(
                "Webhook ignored due to idempotency. Order already marked paid. OrderId={OrderId} Reference={Reference}",
                order.Id,
                reference);

            return;
        }

        _logger.LogInformation(
            "Verifying payment with Paystack. OrderId={OrderId} Reference={Reference}",
            order.Id,
            reference);

        var verification = await _paystackClient.VerifyTransactionAsync(reference);

        if (!verification.Success)
        {
            _logger.LogError(
                "Payment verification failed with Paystack. OrderId={OrderId} Reference={Reference}",
                order.Id,
                reference);

            throw new BadRequestException("Payment verification failed.");
        }

        if (verification.Amount != order.TotalAmount)
        {
            _logger.LogWarning(
                "Payment amount mismatch detected. OrderId={OrderId} ExpectedAmount={ExpectedAmount} ReceivedAmount={ReceivedAmount}",
                order.Id,
                order.TotalAmount,
                verification.Amount);

            throw new BadRequestException("Payment amount mismatch.");
        }

        _logger.LogInformation(
            "Payment verified successfully. OrderId={OrderId} Reference={Reference} Amount={Amount}",
            order.Id,
            reference,
            verification.Amount);

        // FIX: Fetch user safely
        var user = await _userRepository.GetByIdAsync(order.UserId);

        if (user is null)
        {
            _logger.LogError(
                "User not found for paid order. OrderId={OrderId} UserId={UserId}",
                order.Id,
                order.UserId);

            throw new NotFoundException("User not found.");
        }

        order.MarkAsPaid(reference);

        // Create Outbox event
        var payload = JsonSerializer.Serialize(new OrderPaidEvent
        {
            OrderId = order.Id,
            Email = user.Email
        });

        var outboxMessage = new OutboxMessage(
            "OrderPaid",
            payload);

        await _outboxRepository.AddAsync(outboxMessage);

        // Save both order + outbox in same flow
        await _orderRepository.SaveChangesAsync();
        await _outboxRepository.SaveChangesAsync();

        _logger.LogInformation(
            "Order marked as PAID and outbox event created. OrderId={OrderId} Reference={Reference}",
            order.Id,
            reference);
    }
}