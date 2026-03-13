using Ecommerce.Domain.Entities;

namespace Ecommerce.Application.Interfaces;

public interface IDiscountRepository
{
    Task<Discount?> GetActiveDiscountForProductAsync(Guid productId);

    Task AddAsync(Discount discount);

    Task SaveChangesAsync();
}