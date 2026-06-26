using Ecommerce.Application.Common.Exceptions;
using Ecommerce.Application.Common.Pagination;
using Ecommerce.Application.DTOs.Orders;
using Ecommerce.Application.Interfaces;
using Ecommerce.Domain.Entities;
using Ecommerce.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace Ecommerce.Application.Services;

public class AdminOrderService : IAdminOrderService
{
    private readonly IOrderRepository _orderRepository;
    private readonly IUserRepository _userRepository;
    private readonly ILogger<AdminOrderService> _logger;

    public AdminOrderService(
        IOrderRepository orderRepository,
        IUserRepository userRepository,
        ILogger<AdminOrderService> logger)
    {
        _orderRepository = orderRepository;
        _userRepository = userRepository;
        _logger = logger;
    }

    // ─── LIST ALL ORDERS ──────────────────────────────────────────
    public async Task<PagedResult<AdminOrderResponse>> GetAllOrdersAsync(
        OrderQueryParameters query)
    {
        _logger.LogInformation(
            "Admin fetching all orders. Page={Page}, PageSize={PageSize}, " +
            "Status={Status}, DateFrom={From}, DateTo={To}, Email={Email}",
            query.Page, query.PageSize, query.Status,
            query.DateFrom, query.DateTo, query.CustomerEmail);

        var (orders, totalCount) =
            await _orderRepository.GetAllOrdersAsync(query);

        var ordersList = orders.ToList();

        var users = await LoadUsersAsync(
            ordersList.Select(o => o.UserId).Distinct());

        var responses = ordersList
            .Select(o => MapToResponse(o, users))
            .ToList();

        _logger.LogInformation(
            "Admin orders retrieved. ReturnedCount={Count}, TotalCount={Total}",
            responses.Count, totalCount);

        return new PagedResult<AdminOrderResponse>
        {
            Items = responses,
            Page = query.Page,
            PageSize = query.PageSize,
            TotalCount = totalCount
        };
    }

    // ─── GET SINGLE ORDER ────────────────────────────────────────
    public async Task<AdminOrderResponse> GetOrderByIdAsync(Guid orderId)
    {
        _logger.LogInformation(
            "Admin fetching order details. OrderId={OrderId}",
            orderId);

        var order = await _orderRepository.GetByIdAsync(orderId);

        if (order is null)
            throw new NotFoundException("Order not found.");

        var users = await LoadUsersAsync(new[] { order.UserId });

        return MapToResponse(order, users);
    }

    // ─── UPDATE ORDER STATUS ─────────────────────────────────────
    public async Task<AdminOrderResponse> UpdateOrderStatusAsync(
        Guid orderId,
        UpdateOrderStatusRequest request)
    {
        _logger.LogInformation(
            "Admin updating order status. OrderId={OrderId}, NewStatus={Status}",
            orderId, request.Status);

        if (!Enum.TryParse<OrderStatus>(
                request.Status,
                ignoreCase: true,
                out var newStatus))
        {
            throw new BadRequestException(
                $"Invalid status '{request.Status}'. " +
                $"Valid values: {string.Join(", ", Enum.GetNames<OrderStatus>())}");
        }

        var order = await _orderRepository.GetByIdAsync(orderId);

        if (order is null)
            throw new NotFoundException("Order not found.");

        try
        {
            order.UpdateStatus(newStatus);
        }
        catch (InvalidOperationException ex)
        {
            throw new BadRequestException(ex.Message);
        }

        await _orderRepository.UpdateAsync(order);
        await _orderRepository.SaveChangesAsync();

        _logger.LogInformation(
            "Order status updated. OrderId={OrderId}, NewStatus={Status}",
            order.Id, order.Status);

        var users = await LoadUsersAsync(new[] { order.UserId });

        return MapToResponse(order, users);
    }

    // ─── GET DASHBOARD STATS ─────────────────────────────────────
    public async Task<AdminOrderStatsResponse> GetStatsAsync()
    {
        _logger.LogInformation("Admin fetching order stats.");

        var orders = (await _orderRepository.GetAllOrdersForStatsAsync())
            .ToList();

        var now = DateTime.UtcNow;
        var startOfToday = now.Date;
        var startOfWeek = startOfToday.AddDays(-(int)startOfToday.DayOfWeek);
        var startOfMonth = new DateTime(now.Year, now.Month, 1);
        var startOfYear = new DateTime(now.Year, 1, 1);

        // Count stats
        var counts = new OrderCountStats(
            Today: orders.Count(o => o.CreatedAt >= startOfToday),
            ThisWeek: orders.Count(o => o.CreatedAt >= startOfWeek),
            ThisMonth: orders.Count(o => o.CreatedAt >= startOfMonth),
            ThisYear: orders.Count(o => o.CreatedAt >= startOfYear),
            AllTime: orders.Count
        );

        // Revenue stats (only paid orders)
        var paidOrders = orders.Where(o => o.Status == OrderStatus.Paid).ToList();

        var revenue = new OrderRevenueStats(
            Today: paidOrders
                .Where(o => o.CreatedAt >= startOfToday)
                .Sum(o => o.TotalAmount),
            ThisWeek: paidOrders
                .Where(o => o.CreatedAt >= startOfWeek)
                .Sum(o => o.TotalAmount),
            ThisMonth: paidOrders
                .Where(o => o.CreatedAt >= startOfMonth)
                .Sum(o => o.TotalAmount),
            ThisYear: paidOrders
                .Where(o => o.CreatedAt >= startOfYear)
                .Sum(o => o.TotalAmount),
            AllTime: paidOrders.Sum(o => o.TotalAmount)
        );

        // Status breakdown
        var statusBreakdown = new OrderStatusBreakdown(
            Pending: orders.Count(o => o.Status == OrderStatus.Pending),
            Paid: orders.Count(o => o.Status == OrderStatus.Paid),
            Cancelled: orders.Count(o => o.Status == OrderStatus.Cancelled),
            Failed: orders.Count(o =>
                Enum.IsDefined(typeof(OrderStatus), o.Status) &&
                o.Status.ToString().Equals("Failed",
                    StringComparison.OrdinalIgnoreCase))
        );

        // Top 5 products by revenue
        var topProducts = paidOrders
            .SelectMany(o => o.Items)
            .GroupBy(i => new { i.ProductId, i.ProductNameSnapshot })
            .Select(g => new TopProductStat(
                ProductId: g.Key.ProductId,
                ProductName: g.Key.ProductNameSnapshot,
                TotalQuantitySold: g.Sum(i => i.Quantity),
                TotalRevenue: g.Sum(i => i.FinalPriceSnapshot * i.Quantity)
            ))
            .OrderByDescending(p => p.TotalRevenue)
            .Take(5)
            .ToList();

        _logger.LogInformation(
            "Order stats retrieved. TotalOrders={Total}, TotalRevenue={Revenue}",
            counts.AllTime, revenue.AllTime);

        return new AdminOrderStatsResponse(
            counts,
            revenue,
            statusBreakdown,
            topProducts);
    }

    // ─── HELPERS ─────────────────────────────────────────────────
    private async Task<Dictionary<Guid, User>> LoadUsersAsync(
        IEnumerable<Guid> userIds)
    {
        var users = new Dictionary<Guid, User>();

        foreach (var userId in userIds)
        {
            var user = await _userRepository.GetByIdAsync(userId);
            if (user != null)
                users[userId] = user;
        }

        return users;
    }

    private static AdminOrderResponse MapToResponse(
        Order order,
        Dictionary<Guid, User> users)
    {
        users.TryGetValue(order.UserId, out var user);

        var customer = new AdminOrderCustomer(
            order.UserId,
            user?.Email ?? "(unknown)",
            user?.FirstName ?? "(unknown)",
            user?.LastName ?? "(unknown)"
        );

        var items = order.Items.Select(i =>
            new AdminOrderItemResponse(
                i.Id,
                i.ProductId,
                i.ProductNameSnapshot,
                i.OriginalPriceSnapshot,
                i.FinalPriceSnapshot,
                i.Quantity,
                i.FinalPriceSnapshot * i.Quantity
            )).ToList();

        return new AdminOrderResponse(
            order.Id,
            order.TotalAmount,
            order.Status.ToString(),
            order.PaymentReference,
            order.PaidAt,
            order.CreatedAt,
            customer,
            items
        );
    }
}