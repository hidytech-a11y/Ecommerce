using Ecommerce.Application.Interfaces;
using Ecommerce.Domain.Entities;
using Ecommerce.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Ecommerce.Infrastructure.Repositories;

public class DiscountRepository : IDiscountRepository
{
    private readonly AppDbContext _context;

    public DiscountRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<Discount?> GetActiveDiscountForProductAsync(Guid productId)
    {
        var now = DateTime.UtcNow;

        return await _context.Discounts
            .AsNoTracking()
            .Where(d =>
                d.ProductId == productId &&
                d.IsActive &&
                d.StartDate <= now &&
                d.EndDate >= now)
            .FirstOrDefaultAsync();
    }

    public async Task AddAsync(Discount discount)
    {
        await _context.Discounts.AddAsync(discount);
    }

    public async Task SaveChangesAsync()
    {
        await _context.SaveChangesAsync();
    }
}