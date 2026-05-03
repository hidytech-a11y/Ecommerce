using Ecommerce.Application.Interfaces;
using Ecommerce.Domain.Entities;
using Ecommerce.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Ecommerce.Infrastructure.Repositories;

public class CartRepository : ICartRepository
{
    private readonly AppDbContext _context;

    public CartRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<Cart?> GetByUserIdAsync(Guid userId)
    {
        return await _context.Carts
            .Include(c => c.Items)
            .FirstOrDefaultAsync(c => c.UserId == userId);
    }

    public async Task AddAsync(Cart cart)
    {
        await _context.Carts.AddAsync(cart);
    }

    public async Task RemoveItemAsync(Guid userId, Guid productId)
    {
        var cart = await GetByUserIdAsync(userId);

        if (cart is null)
            return;

        var item = cart.Items.FirstOrDefault(i => i.ProductId == productId);

        if (item != null)
        {
            _context.Set<CartItem>().Remove(item);
        }
    }

    public async Task ClearAsync(Guid userId)
    {
        var cart = await GetByUserIdAsync(userId);

        if (cart is null)
            return;

        foreach (var item in cart.Items.ToList())
        {
            _context.Set<CartItem>().Remove(item);
        }
    }

    public async Task SaveChangesAsync()
    {
        await _context.SaveChangesAsync();
    }
}