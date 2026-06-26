namespace Ecommerce.Application.DTOs.Orders;

public record AdminOrderStatsResponse(
    OrderCountStats Counts,
    OrderRevenueStats Revenue,
    OrderStatusBreakdown StatusBreakdown,
    IEnumerable<TopProductStat> TopProducts
);

public record OrderCountStats(
    int Today,
    int ThisWeek,
    int ThisMonth,
    int ThisYear,
    int AllTime
);

public record OrderRevenueStats(
    decimal Today,
    decimal ThisWeek,
    decimal ThisMonth,
    decimal ThisYear,
    decimal AllTime
);

public record OrderStatusBreakdown(
    int Pending,
    int Paid,
    int Cancelled,
    int Failed
);

public record TopProductStat(
    Guid ProductId,
    string ProductName,
    int TotalQuantitySold,
    decimal TotalRevenue
);