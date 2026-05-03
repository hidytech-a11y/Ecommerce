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

    public async Task AddItemAsync(Guid userId, Guid productId, int quantity)
    {
        var cartId = await GetOrCreateCartIdAsync(userId);

        await _context.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO "CartItem" ("Id", "ProductId", "Quantity", "CartId")
            VALUES ({Guid.NewGuid()}, {productId}, {quantity}, {cartId})
            ON CONFLICT ("CartId", "ProductId")
            DO UPDATE SET "Quantity" = "CartItem"."Quantity" + EXCLUDED."Quantity"
            """);
    }

    public async Task RemoveItemAsync(Guid userId, Guid productId)
    {
        var cartId = await _context.Carts
            .Where(c => c.UserId == userId)
            .Select(c => (Guid?)c.Id)
            .FirstOrDefaultAsync();

        if (cartId is null)
            return;

        await _context.Set<CartItem>()
            .Where(i => EF.Property<Guid>(i, "CartId") == cartId.Value && i.ProductId == productId)
            .ExecuteDeleteAsync();
    }

    public async Task ClearAsync(Guid userId)
    {
        var cartId = await _context.Carts
            .Where(c => c.UserId == userId)
            .Select(c => (Guid?)c.Id)
            .FirstOrDefaultAsync();

        if (cartId is null)
            return;

        await _context.Set<CartItem>()
            .Where(i => EF.Property<Guid>(i, "CartId") == cartId.Value)
            .ExecuteDeleteAsync();
    }

    public async Task SaveChangesAsync()
    {
        await _context.SaveChangesAsync();
    }

    private async Task<Guid> GetOrCreateCartIdAsync(Guid userId)
    {
        var cartId = await _context.Carts
            .Where(c => c.UserId == userId)
            .Select(c => (Guid?)c.Id)
            .FirstOrDefaultAsync();

        if (cartId is not null)
            return cartId.Value;

        cartId = Guid.NewGuid();

        await _context.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO "Carts" ("Id", "UserId")
            VALUES ({cartId.Value}, {userId})
            ON CONFLICT ("UserId") DO NOTHING
            """);

        return await _context.Carts
            .Where(c => c.UserId == userId)
            .Select(c => c.Id)
            .FirstAsync();
    }
}
