using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Ecommerce.Application.DTOs.Cart;

namespace Ecommerce.Application.Interfaces;

public interface ICartService
{
    Task AddToCartAsync(Guid userId, Guid productId, int quantity);
    Task RemoveFromCartAsync(Guid userId, Guid productId);
    Task<CartResponse> GetCartAsync(Guid userId);
    Task ClearCartAsync(Guid userId);
}
