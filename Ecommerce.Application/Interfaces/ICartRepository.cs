using Ecommerce.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ecommerce.Application.Interfaces;

public interface ICartRepository
{
    Task<Cart?> GetByUserIdAsync(Guid userId);
    Task AddAsync(Cart cart);
    //Task AddItemAsync(Guid userId, Guid productId, int quantity);
    Task RemoveItemAsync(Guid userId, Guid productId);
    Task ClearAsync(Guid userId);
    Task SaveChangesAsync();
}
