using Ecommerce.Application.Common.Caching;
using Ecommerce.Application.Common.Exceptions;
using Ecommerce.Application.Common.Interfaces;
using Ecommerce.Application.Common.Pagination;
using Ecommerce.Application.Common.Pricing;
using Ecommerce.Application.Common.Utilities;
using Ecommerce.Application.DTOs.Products;
using Ecommerce.Application.Interfaces;
using Ecommerce.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace Ecommerce.Application.Services;

public class ProductService : IProductService
{
    private readonly IProductRepository _repository;
    private readonly IDiscountRepository _discountRepository;
    private readonly ICacheService _cache;
    private readonly ICloudinaryService _cloudinary;
    private readonly ICartRepository _cartRepository;        
    private readonly ILogger<ProductService> _logger;       

    public ProductService(
        IProductRepository repository,
        IDiscountRepository discountRepository,
        ICacheService cache,
        ICloudinaryService cloudinary,
        ICartRepository cartRepository,                       
        ILogger<ProductService> logger)                       
    {
        _repository = repository;
        _discountRepository = discountRepository;
        _cache = cache;
        _cloudinary = cloudinary;
        _cartRepository = cartRepository;                    
        _logger = logger;                                     
    }

    public async Task<ProductResponse> CreateProductAsync(CreateProductRequest request)
    {
        var slug = await GenerateUniqueSlugAsync(request.Name);

        var product = new Product(
            request.Name,
            slug,
            request.Description,
            request.Price,
            request.StockQuantity,
            request.CategoryId);

        await _repository.AddAsync(product);
        await _repository.SaveChangesAsync();

        return MapToResponse(product, null);
    }

    public async Task<ProductResponse> UpdateProductAsync(Guid id, UpdateProductRequest request)
    {
        var product = await _repository.GetTrackedByIdAsync(id);

        if (product is null)
            throw new NotFoundException("Product not found.");

        product.Update(
            request.Name,
            request.Description,
            request.Price,
            request.StockQuantity,
            request.IsAvailable);

        await _repository.SaveChangesAsync();

        await _cache.RemoveAsync(CacheKeys.ProductDetails(id));

        return MapToResponse(product, null);
    }

    // Removes cart items first 
    public async Task DeleteProductAsync(Guid id)
    {
 
        var product = await _repository.GetTrackedByIdAsync(id);

        if (product is null)
            throw new NotFoundException("Product not found.");

        _logger.LogInformation(
            "Deleting product. ProductId={ProductId} Name={Name}",
            product.Id, product.Name);

        try
        {
            await _cartRepository.RemoveCartItemsByProductIdAsync(id);

            _logger.LogInformation(
                "Removed product from all carts. ProductId={ProductId}",
                product.Id);

            if (!string.IsNullOrWhiteSpace(product.FrontImagePublicId))
                await _cloudinary.DeleteAsync(product.FrontImagePublicId);

            if (!string.IsNullOrWhiteSpace(product.BackImagePublicId))
                await _cloudinary.DeleteAsync(product.BackImagePublicId);

            if (!string.IsNullOrWhiteSpace(product.SideImagePublicId))
                await _cloudinary.DeleteAsync(product.SideImagePublicId);

            await _repository.DeleteAsync(product);

            await _repository.SaveChangesAsync();

            await _cache.RemoveAsync(CacheKeys.ProductDetails(id));

            _logger.LogInformation(
                "Product deleted successfully. ProductId={ProductId}",
                product.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to delete product. ProductId={ProductId}",
                product.Id);
            throw;
        }
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
            product.Price, discount);

        var response = MapToResponse(product, finalPrice);

        await _cache.SetAsync(cacheKey, response, TimeSpan.FromMinutes(10));

        return response;
    }

    public async Task<ProductResponse> GetProductBySlugAsync(string slug)
    {
        if (string.IsNullOrWhiteSpace(slug))
            throw new BadRequestException("Slug is required.");

        var cacheKey = CacheKeys.ProductBySlug(slug);
        var cached = await _cache.GetAsync<ProductResponse>(cacheKey);

        if (cached != null)
            return cached;

        var product = await _repository.GetBySlugAsync(slug);

        if (product is null)
            throw new NotFoundException("Product not found.");

        var discount = await _discountRepository
            .GetActiveDiscountForProductAsync(product.Id);

        var finalPrice = DiscountCalculator.CalculateFinalPrice(
            product.Price, discount);

        var response = MapToResponse(product, finalPrice);

        await _cache.SetAsync(cacheKey, response, TimeSpan.FromMinutes(10));

        return response;
    }

    public async Task<PagedResult<ProductResponse>> GetProductsAsync(ProductQueryParameters query)
    {
        var cacheKey = CacheKeys.ProductsPage(
            query.Page, query.PageSize, query.Search);

        var cachedProducts =
            await _cache.GetAsync<PagedResult<ProductResponse>>(cacheKey);

        if (cachedProducts != null)
            return cachedProducts;

        var (items, totalCount) = await _repository.GetProductsAsync(query);

        var responses = items.Select(p => MapToResponse(p, null)).ToList();

        var result = new PagedResult<ProductResponse>
        {
            Items = responses,
            Page = query.Page,
            PageSize = query.PageSize,
            TotalCount = totalCount
        };

        await _cache.SetAsync(cacheKey, result, TimeSpan.FromMinutes(5));

        return result;
    }

    public async Task<ProductResponse> UpdateProductImagesAsync(
        Guid id,
        UpdateProductImagesInput input)
    {
        var product = await _repository.GetTrackedByIdAsync(id);

        if (product is null)
            throw new NotFoundException("Product not found.");

        var allowedTypes = new[] { "image/jpeg", "image/png", "image/webp" };
        const long maxBytes = 5 * 1024 * 1024;

        async Task ProcessImage(
            ImageFileInput? file,
            bool remove,
            string? oldPublicId,
            Action<string?, string?> setter)
        {
            if (remove)
            {
                if (!string.IsNullOrWhiteSpace(oldPublicId))
                    await _cloudinary.DeleteAsync(oldPublicId);
                setter(null, null);
                return;
            }

            if (file is null) return;

            if (!allowedTypes.Contains(file.ContentType, StringComparer.OrdinalIgnoreCase))
                throw new InvalidOperationException(
                    $"'{file.FileName}' has an invalid type '{file.ContentType}'.");

            if (file.Content.Length > maxBytes)
                throw new InvalidOperationException(
                    $"'{file.FileName}' exceeds the 5 MB limit.");

            if (!string.IsNullOrWhiteSpace(oldPublicId))
                await _cloudinary.DeleteAsync(oldPublicId);

            var folder = $"ecommerce/products/{product.Id}";

            (string uploadedUrl, string uploadedPublicId) =
                await _cloudinary.UploadAsync(
                    file.Content, file.FileName, file.ContentType, folder);

            setter(uploadedUrl, uploadedPublicId);
        }

        await ProcessImage(input.Front, input.RemoveFront,
            product.FrontImagePublicId, product.SetFrontImage);

        await ProcessImage(input.Back, input.RemoveBack,
            product.BackImagePublicId, product.SetBackImage);

        await ProcessImage(input.Side, input.RemoveSide,
            product.SideImagePublicId, product.SetSideImage);

        await _repository.UpdateAsync(product);
        await _repository.SaveChangesAsync();

        await _cache.RemoveAsync(CacheKeys.ProductDetails(id));

        return MapToResponse(product, null);
    }

    //  HELPERS
    private async Task<string> GenerateUniqueSlugAsync(string name)
    {
        var baseSlug = SlugGenerator.Generate(name);

        if (string.IsNullOrWhiteSpace(baseSlug))
            baseSlug = "product";

        var slug = baseSlug;
        var counter = 2;

        while (await _repository.SlugExistsAsync(slug))
        {
            slug = $"{baseSlug}-{counter}";
            counter++;
        }

        return slug;
    }

    private static ProductResponse MapToResponse(Product product, decimal? finalPrice)
    {
        var price = finalPrice ?? product.Price;

        return new ProductResponse(
            product.Id,
            product.Name,
            product.Slug,
            product.Description,
            product.Price,
            price,
            price < product.Price,
            product.StockQuantity,
            product.IsAvailable,
            product.CategoryId.ToString(),
            product.FrontImageUrl,
            product.BackImageUrl,
            product.SideImageUrl);
    }
}