using Ecommerce.Application.Common.Interfaces;
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

}