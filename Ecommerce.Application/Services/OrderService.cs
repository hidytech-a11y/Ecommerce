using Ecommerce.Application.Common.Exceptions;
using Ecommerce.Application.Common.Pricing;
using Ecommerce.Application.DTOs.Orders;
using Ecommerce.Application.Events;
using Ecommerce.Application.Interfaces;
using Ecommerce.Domain.Entities;
using Ecommerce.Domain.Enums;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace Ecommerce.Application.Services;

public class OrderService : IOrderService
{
    private readonly IOrderRepository _orderRepository;
    private readonly IProductRepository _productRepository;
    private readonly IDiscountRepository _discountRepository;
    private readonly ICartRepository _cartRepository;
    private readonly IUserRepository _userRepository;
    private readonly IOutboxRepository _outboxRepository;
    private readonly ILogger<OrderService> _logger;

    public OrderService(
        IOrderRepository orderRepository,
        IProductRepository productRepository,
        IDiscountRepository discountRepository,
        ICartRepository cartRepository,
        IUserRepository userRepository,
        IOutboxRepository outboxRepository,
        ILogger<OrderService> logger)
    {
        _orderRepository = orderRepository;
        _productRepository = productRepository;
        _discountRepository = discountRepository;
        _cartRepository = cartRepository;
        _userRepository = userRepository;
        _outboxRepository = outboxRepository;
        _logger = logger;
    }

    public async Task<OrderResponse> CreateOrderFromCartAsync(Guid userId)
    {
        await using var transaction = await _orderRepository.BeginTransactionAsync();

        try
        {
            var cart = await _cartRepository.GetByUserIdAsync(userId);

            if (cart is null || !cart.Items.Any())
                throw new BadRequestException("Cart is empty.");

            var order = new Order(userId);

            _logger.LogInformation(
                "Starting checkout from cart. OrderId={OrderId} UserId={UserId}",
                order.Id, userId);

            foreach (var item in cart.Items)
            {
                var product = await _productRepository.GetByIdAsync(item.ProductId);

                if (product is null)
                    throw new NotFoundException("Product not found.");

                if (product.StockQuantity < item.Quantity)
                    throw new BadRequestException(
                        $"Insufficient stock for {product.Name}");

                var discount = await _discountRepository
                    .GetActiveDiscountForProductAsync(product.Id);

                var finalPrice = DiscountCalculator.CalculateFinalPrice(
                    product.Price, discount);

                var orderItem = new OrderItem(
                    product.Id,
                    product.Name,
                    product.Price,
                    finalPrice,
                    item.Quantity);

                order.AddItem(orderItem);
                product.ReduceStock(item.Quantity);
            }

            await _orderRepository.AddAsync(order);
            cart.Clear();

            await _orderRepository.SaveChangesAsync();
            await transaction.CommitAsync();

            _logger.LogInformation(
                "Order successfully created from cart. OrderId={OrderId}",
                order.Id);

            return MapToResponse(order);
        }
        catch (ConcurrencyException)
        {
            await transaction.RollbackAsync();
            throw new BadRequestException(
                "Inventory changed during checkout. Please retry.");
        }
    }

    public async Task<IEnumerable<OrderResponse>> GetUserOrdersAsync(Guid userId)
    {
        _logger.LogInformation(
            "Fetching orders for user. UserId={UserId}", userId);

        var orders = await _orderRepository.GetUserOrdersAsync(userId);

        _logger.LogInformation(
            "Orders retrieved for user. UserId={UserId} OrderCount={Count}",
            userId, orders.Count());

        return orders.Select(MapToResponse);
    }

    public async Task<OrderResponse> GetOrderAsync(Guid orderId, Guid userId)
    {
        _logger.LogInformation(
            "Fetching order details. OrderId={OrderId} RequestedByUser={UserId}",
            orderId, userId);

        var order = await _orderRepository.GetByIdAsync(orderId);

        if (order is null)
            throw new NotFoundException("Order not found.");

        if (order.UserId != userId)
        {
            _logger.LogWarning(
                "Unauthorized order access attempt. OrderId={OrderId} " +
                "OwnerUserId={OwnerUserId} RequestUserId={RequestUserId}",
                order.Id, order.UserId, userId);

            throw new UnauthorizedException("Access denied.");
        }

        return MapToResponse(order);
    }

    //  USER CANCEL 
    public async Task<OrderResponse> CancelOrderAsync(
        Guid orderId,
        Guid userId,
        CancelOrderRequest request)
    {
        _logger.LogInformation(
            "User attempting to cancel order. OrderId={OrderId} UserId={UserId}",
            orderId, userId);

        var order = await _orderRepository.GetByIdAsync(orderId);

        if (order is null)
            throw new NotFoundException("Order not found.");

        if (order.UserId != userId)
        {
            _logger.LogWarning(
                "Unauthorized cancel attempt. OrderId={OrderId} " +
                "OwnerUserId={OwnerUserId} RequestUserId={RequestUserId}",
                order.Id, order.UserId, userId);

            throw new UnauthorizedException("Access denied.");
        }

        return await ExecuteCancellationAsync(order, userId, request);
    }

    //  ADMIN CANCEL 
    public async Task<OrderResponse> AdminCancelOrderAsync(
        Guid orderId,
        Guid adminId,
        CancelOrderRequest request)
    {
        _logger.LogInformation(
            "Admin attempting to cancel order. OrderId={OrderId} AdminId={AdminId}",
            orderId, adminId);

        var order = await _orderRepository.GetByIdAsync(orderId);

        if (order is null)
            throw new NotFoundException("Order not found.");

        return await ExecuteCancellationAsync(order, adminId, request);
    }

    // SHARED CANCELLATION LOGIC
    private async Task<OrderResponse> ExecuteCancellationAsync(
        Order order,
        Guid cancelledBy,
        CancelOrderRequest request)
    {
        // Idempotent — already cancelled
        if (order.Status == OrderStatus.Cancelled)
        {
            _logger.LogInformation(
                "Order already cancelled. Returning current state. OrderId={OrderId}",
                order.Id);

            return MapToResponse(order);
        }

        // Only pending orders can be cancelled
        if (order.Status != OrderStatus.Pending)
            throw new BadRequestException(
                $"Only pending orders can be cancelled. " +
                $"Current status: {order.Status}");

        await using var transaction = await _orderRepository.BeginTransactionAsync();

        try
        {
            // Restore stock for each item
            foreach (var item in order.Items)
            {
                var product = await _productRepository
                    .GetTrackedByIdAsync(item.ProductId);

                if (product is not null)
                {
                    product.RestoreStock(item.Quantity);
                    await _productRepository.UpdateAsync(product);

                    _logger.LogInformation(
                        "Stock restored. ProductId={ProductId} Qty={Qty}",
                        product.Id, item.Quantity);
                }
                else
                {
                    _logger.LogWarning(
                        "Product not found while restoring stock. " +
                        "ProductId={ProductId}",
                        item.ProductId);
                }
            }

            // Cancel the order
            try
            {
                order.Cancel(cancelledBy, request.Reason);
            }
            catch (InvalidOperationException ex)
            {
                throw new BadRequestException(ex.Message);
            }

            await _orderRepository.UpdateAsync(order);

            // Get order owner's email for the event
            var orderOwner = await _userRepository.GetByIdAsync(order.UserId)
                             ?? throw new NotFoundException("User not found.");

            // Publish outbox event
            var payload = JsonSerializer.Serialize(new OrderCancelledEvent
            {
                OrderId = order.Id,
                UserId = order.UserId,
                Email = orderOwner.Email,
                Reason = request.Reason,
                CancelledAt = order.CancelledAt!.Value
            });

            await _outboxRepository.AddAsync(
                new OutboxMessage("OrderCancelled", payload));

            await _orderRepository.SaveChangesAsync();
            await _outboxRepository.SaveChangesAsync();

            await transaction.CommitAsync();

            _logger.LogInformation(
                "Order cancelled successfully. OrderId={OrderId} CancelledBy={CancelledBy} Reason={Reason}",
                order.Id, cancelledBy, request.Reason ?? "(none)");

            return MapToResponse(order);
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    //HELPER 
    private static OrderResponse MapToResponse(Order order)
    {
        return new OrderResponse(
            order.Id,
            order.TotalAmount,
            order.Status.ToString(),
            order.CreatedAt,
            order.CancelledAt,
            order.CancellationReason
        );
    }
}