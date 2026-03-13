using Ecommerce.Domain.Entities;

namespace Ecommerce.Application.Interfaces;

public interface IOutboxRepository
{
    Task AddAsync(OutboxMessage message);

    Task SaveChangesAsync();
}