using Ecommerce.Application.DTOs.Products;
using Ecommerce.Domain.Entities;

namespace Ecommerce.Application.Interfaces;

public interface IProductRepository
{
    Task<Product?> GetByIdAsync(Guid id);
    Task<Product?> GetTrackedByIdAsync(Guid id);

    Task<Product?> GetBySlugAsync(string slug);

    Task<bool> SlugExistsAsync(string slug);

    Task AddAsync(Product product);
    Task UpdateAsync(Product product);
    Task DeleteAsync(Product product);

    Task<(IEnumerable<Product> Items, int TotalCount)>
        GetProductsAsync(ProductQueryParameters query);

    Task SaveChangesAsync();
}