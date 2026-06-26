using Ecommerce.Application.Common.Interfaces;
using Ecommerce.Application.DTOs.Orders;
using Ecommerce.Domain.Entities;

namespace Ecommerce.Application.Interfaces;

public interface IOrderRepository
{
    Task AddAsync(Order order);

    Task<Order?> GetByIdAsync(Guid id);

    Task<IEnumerable<Order>> GetUserOrdersAsync(Guid userId);

    Task SaveChangesAsync();

    Task<ITransaction> BeginTransactionAsync();

    Task<Order?> GetByPaymentReferenceAsync(string reference);

    Task<IEnumerable<Order>> GetPendingPaymentOrdersAsync();

    Task<(IEnumerable<Order> Items, int TotalCount)>GetAllOrdersAsync(OrderQueryParameters query);

    // For admin stats
    Task<IEnumerable<Order>> GetAllOrdersForStatsAsync();

    // Update tracking for status changes
    Task UpdateAsync(Order order);
}