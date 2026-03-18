using Ecommerce.Domain.Entities;

namespace Ecommerce.Application.Interfaces;

public interface IUserRepository
{
    Task<bool> ExistsByEmailAsync(string email);
    Task<User?> GetByEmailAsync(string email);

    Task AddAsync(User user);
    Task SaveChangesAsync();

    Task<User?> GetByIdAsync(Guid id);
}