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
    private readonly ILogger<OrderService> _logger;

    public OrderService(
        IOrderRepository orderRepository,
        IProductRepository productRepository,
        IDiscountRepository discountRepository,
        ILogger<OrderService> logger)
    {
        _orderRepository = orderRepository;
        _productRepository = productRepository;
        _discountRepository = discountRepository;
        _logger = logger;
    }

    public async Task<OrderResponse> CreateOrderAsync(Guid userId, CreateOrderRequest request)
    {
        await using var transaction = await _orderRepository.BeginTransactionAsync();

        try
        {
            var order = new Order(userId);

            _logger.LogInformation(
                "Starting order creation. OrderId={OrderId} UserId={UserId}",
                order.Id,
                userId);

            foreach (var item in request.Items)
            {
                _logger.LogInformation(
                    "Checking inventory for ProductId={ProductId} RequestedQuantity={Quantity}",
                    item.ProductId,
                    item.Quantity);

                var product = await _productRepository.GetByIdAsync(item.ProductId);

                if (product is null)
                {
                    _logger.LogWarning(
                        "Product not found during checkout. ProductId={ProductId}",
                        item.ProductId);

                    throw new NotFoundException($"Product {item.ProductId} not found.");
                }

                if (product.StockQuantity < item.Quantity)
                {
                    _logger.LogWarning(
                        "Insufficient stock detected. ProductId={ProductId} Available={AvailableStock} Requested={Requested}",
                        product.Id,
                        product.StockQuantity,
                        item.Quantity);

                    throw new BadRequestException($"Insufficient stock for {product.Name}");
                }

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

                _logger.LogInformation(
                    "Inventory reserved. ProductId={ProductId} Quantity={Quantity} RemainingStock={RemainingStock}",
                    product.Id,
                    item.Quantity,
                    product.StockQuantity);
            }

            await _orderRepository.AddAsync(order);
            await _orderRepository.SaveChangesAsync();

           

            await transaction.CommitAsync();

            _logger.LogInformation(
                "Order successfully created. OrderId={OrderId} UserId={UserId} TotalAmount={TotalAmount}",
                order.Id,
                userId,
                order.TotalAmount);

            return new OrderResponse(
                order.Id,
                order.TotalAmount,
                order.Status.ToString(),
                order.CreatedAt);
        }
        catch (ConcurrencyException ex)
        {
            await transaction.RollbackAsync();

            _logger.LogWarning(
                ex,
                "Concurrency conflict during checkout. UserId={UserId}",
                userId);

            throw new BadRequestException(
                "Inventory changed during checkout. Please retry.");
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();

            _logger.LogError(
                ex,
                "Unexpected error occurred during order creation. UserId={UserId}",
                userId);

            throw;
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