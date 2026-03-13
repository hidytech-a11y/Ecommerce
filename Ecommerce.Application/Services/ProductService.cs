using Ecommerce.Application.Common.Caching;
using Ecommerce.Application.Common.Exceptions;
using Ecommerce.Application.Common.Interfaces;
using Ecommerce.Application.Common.Pagination;
using Ecommerce.Application.Common.Pricing;
using Ecommerce.Application.DTOs.Products;
using Ecommerce.Application.Interfaces;
using Ecommerce.Domain.Entities;

namespace Ecommerce.Application.Services;

public class ProductService : IProductService
{
    private readonly IProductRepository _repository;
    private readonly IDiscountRepository _discountRepository;
    private readonly ICacheService _cache;

    public ProductService(
        IProductRepository repository,
        IDiscountRepository discountRepository,
        ICacheService cache)
    {
        _repository = repository;
        _discountRepository = discountRepository;
        _cache = cache;
    }

    public async Task<ProductResponse> CreateProductAsync(CreateProductRequest request)
    {
        var product = new Product(
            request.Name,
            request.Description,
            request.Price,
            request.StockQuantity,
            request.CategoryId);

        await _repository.AddAsync(product);
        await _repository.SaveChangesAsync();

        // Invalidate catalog cache by letting TTL expire naturally
        // No direct delete since catalog keys are paginated

        return MapToResponse(product, null);
    }

    public async Task<ProductResponse> UpdateProductAsync(Guid id, UpdateProductRequest request)
    {
        var product = await _repository.GetByIdAsync(id);

        if (product is null)
            throw new NotFoundException("Product not found.");

        product.Update(
            request.Name,
            request.Description,
            request.Price,
            request.StockQuantity,
            request.IsAvailable);

        await _repository.UpdateAsync(product);
        await _repository.SaveChangesAsync();

        // Remove cached product details
        await _cache.RemoveAsync(CacheKeys.ProductDetails(id));

        return MapToResponse(product, null);
    }

    public async Task DeleteProductAsync(Guid id)
    {
        var product = await _repository.GetByIdAsync(id);

        if (product is null)
            throw new NotFoundException("Product not found.");

        await _repository.DeleteAsync(product);
        await _repository.SaveChangesAsync();

        // Remove cached product details
        await _cache.RemoveAsync(CacheKeys.ProductDetails(id));
    }

    public async Task<ProductResponse> GetProductAsync(Guid id)
    {
        var cacheKey = CacheKeys.ProductDetails(id);

        var cached = await _cache.GetAsync<ProductResponse>(cacheKey);

        if (cached != null)
            return cached;

        var product = await _repository.GetByIdAsync(id);

        if (product is null)
            throw new NotFoundException("Product not found.");

        var discount = await _discountRepository
            .GetActiveDiscountForProductAsync(product.Id);

        var finalPrice = DiscountCalculator.CalculateFinalPrice(
            product.Price,
            discount);

        var response = MapToResponse(product, finalPrice);

        // Cache product details
        await _cache.SetAsync(
            cacheKey,
            response,
            TimeSpan.FromMinutes(10));

        return response;
    }

    public async Task<PagedResult<ProductResponse>> GetProductsAsync(ProductQueryParameters query)
    {
        var cacheKey = CacheKeys.ProductsPage(
            query.Page,
            query.PageSize,
            query.Search);

        var cachedProducts =
            await _cache.GetAsync<PagedResult<ProductResponse>>(cacheKey);

        if (cachedProducts != null)
            return cachedProducts;

        var (items, totalCount) = await _repository.GetProductsAsync(query);

        var responses = items.Select(p =>
            MapToResponse(p, null)).ToList();

        var result = new PagedResult<ProductResponse>
        {
            Items = responses,
            Page = query.Page,
            PageSize = query.PageSize,
            TotalCount = totalCount
        };

        await _cache.SetAsync(
            cacheKey,
            result,
            TimeSpan.FromMinutes(5));

        return result;
    }

    private static ProductResponse MapToResponse(Product product, decimal? finalPrice)
    {
        var price = finalPrice ?? product.Price;

        return new ProductResponse(
            product.Id,
            product.Name,
            product.Description,
            product.Price,
            price,
            price < product.Price,
            product.StockQuantity,
            product.IsAvailable,
            ""
        );
    }
}