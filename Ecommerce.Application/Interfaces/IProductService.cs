using Ecommerce.Application.Common.Pagination;
using Ecommerce.Application.DTOs.Products;

namespace Ecommerce.Application.Interfaces;

public interface IProductService
{
    Task<ProductResponse> CreateProductAsync(CreateProductRequest request);

    Task<ProductResponse> UpdateProductAsync(Guid id, UpdateProductRequest request);

    Task DeleteProductAsync(Guid id);

    Task<ProductResponse> GetProductAsync(Guid id);

    Task<PagedResult<ProductResponse>> GetProductsAsync(ProductQueryParameters query);
}