using Ecommerce.Application.Common.Exceptions;
using Ecommerce.Application.Common.Pricing;
using Ecommerce.Application.DTOs.Orders;
using Ecommerce.Application.Interfaces;
using Ecommerce.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace Ecommerce.Application.Services;

public class OrderService : IOrderService
{
    private readonly IOrderRepository _orderRepository;
    private readonly IProductRepository _productRepository;
    private readonly IDiscountRepository _discountRepository;
    private readonly ICartRepository _cartRepository;
    private readonly ILogger<OrderService> _logger;

    public OrderService(
        IOrderRepository orderRepository,
        IProductRepository productRepository,
        IDiscountRepository discountRepository,
        ICartRepository cartRepository,
        ILogger<OrderService> logger)
    {
        _orderRepository = orderRepository;
        _productRepository = productRepository;
        _discountRepository = discountRepository;
        _cartRepository = cartRepository;
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
                order.Id,
                userId);

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
                    product.Price,
                    discount);

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

            // Clear cart after successful order creation
            cart.Clear();

            await _orderRepository.SaveChangesAsync();

            await transaction.CommitAsync();

            _logger.LogInformation(
                "Order successfully created from cart. OrderId={OrderId}",
                order.Id);

            return new OrderResponse(
                order.Id,
                order.TotalAmount,
                order.Status.ToString(),
                order.CreatedAt);
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
            "Fetching orders for user. UserId={UserId}",
            userId);

        var orders = await _orderRepository.GetUserOrdersAsync(userId);

        _logger.LogInformation(
            "Orders retrieved for user. UserId={UserId} OrderCount={Count}",
            userId,
            orders.Count());

        return orders.Select(o =>
            new OrderResponse(
                o.Id,
                o.TotalAmount,
                o.Status.ToString(),
                o.CreatedAt
            ));
    }

    public async Task<OrderResponse> GetOrderAsync(Guid orderId, Guid userId)
    {
        _logger.LogInformation(
            "Fetching order details. OrderId={OrderId} RequestedByUser={UserId}",
            orderId,
            userId);

        var order = await _orderRepository.GetByIdAsync(orderId);

        if (order is null)
        {
            _logger.LogWarning(
                "Order not found. OrderId={OrderId}",
                orderId);

            throw new NotFoundException("Order not found.");
        }

        if (order.UserId != userId)
        {
            _logger.LogWarning(
                "Unauthorized order access attempt. OrderId={OrderId} OwnerUserId={OwnerUserId} RequestUserId={RequestUserId}",
                order.Id,
                order.UserId,
                userId);

            throw new UnauthorizedException("Access denied.");
        }

        _logger.LogInformation(
            "Order details retrieved successfully. OrderId={OrderId}",
            order.Id);

        return new OrderResponse(
            order.Id,
            order.TotalAmount,
            order.Status.ToString(),
            order.CreatedAt);
    }
}