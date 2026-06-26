using Ecommerce.Application.Common.Exceptions;
using Ecommerce.Application.Common.Interfaces;
using Ecommerce.Application.DTOs.Orders;
using Ecommerce.Application.Interfaces;
using Ecommerce.Domain.Entities;
using Ecommerce.Domain.Enums;
using Ecommerce.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Ecommerce.Infrastructure.Repositories;

public class OrderRepository : IOrderRepository
{
    private readonly AppDbContext _context;

    public OrderRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(Order order)
    {
        await _context.Orders.AddAsync(order);
    }

    public async Task<Order?> GetByIdAsync(Guid id)
    {
        return await _context.Orders
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.Id == id);
    }

    public async Task<IEnumerable<Order>> GetUserOrdersAsync(Guid userId)
    {
        return await _context.Orders
            .Where(o => o.UserId == userId)
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync();
    }

    public async Task SaveChangesAsync()
    {
        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException ex)
        {
            throw new ConcurrencyException("A concurrency conflict occurred.");
        }
    }

    public async Task<ITransaction> BeginTransactionAsync()
    {
        var transaction = await _context.Database.BeginTransactionAsync();
        return new EfTransaction(transaction);
    }

    public async Task<Order?> GetByPaymentReferenceAsync(string reference)
    {
        return await _context.Orders
            .FirstOrDefaultAsync(o => o.PaymentReference == reference);
    }

    public async Task<IEnumerable<Order>> GetPendingPaymentOrdersAsync()
    {
        return await _context.Orders
            .Where(o =>
                o.Status == OrderStatus.Pending &&
                o.PaymentReference != null)
            .ToListAsync();
    }

    public async Task<(IEnumerable<Order> Items, int TotalCount)>
       GetAllOrdersAsync(OrderQueryParameters query)
    {
        var ordersQuery = _context.Orders
            .Include(o => o.Items)
            .AsNoTracking()
            .AsQueryable();

        // Filter by status (case-insensitive)
        if (!string.IsNullOrWhiteSpace(query.Status))
        {
            if (Enum.TryParse<OrderStatus>(
                    query.Status,
                    ignoreCase: true,
                    out var parsedStatus))
            {
                ordersQuery = ordersQuery
                    .Where(o => o.Status == parsedStatus);
            }
        }

        // Filter by date range
        if (query.DateFrom.HasValue)
        {
            ordersQuery = ordersQuery
                .Where(o => o.CreatedAt >= query.DateFrom.Value);
        }

        if (query.DateTo.HasValue)
        {
            ordersQuery = ordersQuery
                .Where(o => o.CreatedAt <= query.DateTo.Value);
        }

        // Filter by payment reference
        if (!string.IsNullOrWhiteSpace(query.PaymentReference))
        {
            ordersQuery = ordersQuery
                .Where(o => o.PaymentReference != null &&
                            o.PaymentReference.Contains(query.PaymentReference));
        }

        // Filter by customer email (join with Users)
        if (!string.IsNullOrWhiteSpace(query.CustomerEmail))
        {
            var matchingUserIds = await _context.Users
                .Where(u => u.Email.Contains(query.CustomerEmail))
                .Select(u => u.Id)
                .ToListAsync();

            ordersQuery = ordersQuery
                .Where(o => matchingUserIds.Contains(o.UserId));
        }

        var totalCount = await ordersQuery.CountAsync();

        var items = await ordersQuery
            .OrderByDescending(o => o.CreatedAt)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync();

        return (items, totalCount);
    }

    public async Task<IEnumerable<Order>> GetAllOrdersForStatsAsync()
    {
        return await _context.Orders
            .Include(o => o.Items)
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task UpdateAsync(Order order)
    {
        _context.Orders.Update(order);
        await Task.CompletedTask;
    }
}