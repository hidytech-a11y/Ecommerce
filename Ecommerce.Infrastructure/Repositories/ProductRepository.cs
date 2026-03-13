using Ecommerce.Application.DTOs.Products;
using Ecommerce.Application.Interfaces;
using Ecommerce.Domain.Entities;
using Ecommerce.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Ecommerce.Infrastructure.Repositories;

public class ProductRepository : IProductRepository
{
    private readonly AppDbContext _context;

    public ProductRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<Product?> GetByIdAsync(Guid id)
    {
        return await _context.Products
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == id);
    }

    public async Task AddAsync(Product product)
    {
        await _context.Products.AddAsync(product);
    }

    public async Task UpdateAsync(Product product)
    {
        _context.Products.Update(product);
        await Task.CompletedTask;
    }

    public async Task DeleteAsync(Product product)
    {
        _context.Products.Remove(product);
        await Task.CompletedTask;
    }

    public async Task<(IEnumerable<Product> Items, int TotalCount)>
        GetProductsAsync(ProductQueryParameters query)
    {
        var productsQuery = _context.Products
            .AsNoTracking()
            .AsQueryable();

        // 🔍 Search by name
        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            productsQuery = productsQuery
                .Where(p => p.Name.Contains(query.Search));
        }

        // 📂 Filter by category
        if (query.CategoryId.HasValue)
        {
            productsQuery = productsQuery
                .Where(p => p.CategoryId == query.CategoryId.Value);
        }

        // 💰 Price filtering
        if (query.MinPrice.HasValue)
        {
            productsQuery = productsQuery
                .Where(p => p.Price >= query.MinPrice.Value);
        }

        if (query.MaxPrice.HasValue)
        {
            productsQuery = productsQuery
                .Where(p => p.Price <= query.MaxPrice.Value);
        }

        var totalCount = await productsQuery.CountAsync();

        var items = await productsQuery
            .OrderBy(p => p.Name)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync();

        return (items, totalCount);
    }

    public async Task SaveChangesAsync()
    {
        await _context.SaveChangesAsync();
    }
}