using Ecommerce.Application.Common.Caching;
using Ecommerce.Application.Common.Exceptions;
using Ecommerce.Application.Common.Interfaces;
using Ecommerce.Application.Common.Pagination;
using Ecommerce.Application.Common.Pricing;
using Ecommerce.Application.DTOs.Products;
using Ecommerce.Application.Interfaces;
using Ecommerce.Domain.Entities;
using Microsoft.AspNetCore.Http;

namespace Ecommerce.Application.Services;

public class ProductService : IProductService
{
    private readonly IProductRepository _repository;
    private readonly IDiscountRepository _discountRepository;
    private readonly ICacheService _cache;
    private readonly ICloudinaryService _cloudinary;

    public ProductService(
        IProductRepository repository,
        IDiscountRepository discountRepository,
        ICacheService cache, ICloudinaryService cloudinary)
    {
        _repository = repository;
        _discountRepository = discountRepository;
        _cache = cache;
        _cloudinary = cloudinary;
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

    public async Task<ProductResponse> CreateProductAsync(
    CreateProductRequest request)
    {
        var product = new Product(
            request.Name,
            request.Description,
            request.Price,
            request.StockQuantity,
            request.CategoryId);

        var folder = $"ecommerce/products/{Guid.NewGuid()}";

        if (request.FrontImage != null)
        {
            using var stream = request.FrontImage.OpenReadStream();

            var (url, publicId) =
                await _cloudinary.UploadAsync(
                    stream,
                    request.FrontImage.FileName,
                    request.FrontImage.ContentType,
                    folder);

            product.SetFrontImage(url, publicId);
        }

        if (request.BackImage != null)
        {
            using var stream = request.BackImage.OpenReadStream();

            var (url, publicId) =
                await _cloudinary.UploadAsync(
                    stream,
                    request.BackImage.FileName,
                    request.BackImage.ContentType,
                    folder);

            product.SetBackImage(url, publicId);
        }

        if (request.SideImage != null)
        {
            using var stream = request.SideImage.OpenReadStream();

            var (url, publicId) =
                await _cloudinary.UploadAsync(
                    stream,
                    request.SideImage.FileName,
                    request.SideImage.ContentType,
                    folder);

            product.SetSideImage(url, publicId);
        }

        await _repository.AddAsync(product);
        await _repository.SaveChangesAsync();

        return MapToResponse(product, null);
    }

    public async Task<PagedResult<ProductResponse>> GetProductsAsync(
    ProductQueryParameters query)
    {
        var cacheKey = CacheKeys.ProductsPage(
            query.Page,
            query.PageSize,
            query.Search);

        var cachedProducts =
            await _cache.GetAsync<PagedResult<ProductResponse>>(cacheKey);

        if (cachedProducts != null)
            return cachedProducts;

        var (items, totalCount) =
            await _repository.GetProductsAsync(query);

        var responses = items
            .Select(p => MapToResponse(p, null))
            .ToList();

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
            "",
            product.FrontImageUrl,
            product.BackImageUrl,
            product.SideImageUrl
        );
    }

    public async Task<ProductResponse> UpdateProductImagesAsync(Guid id, UpdateProductImagesInput input)
    {
        var product = await _repository.GetTrackedByIdAsync(id);
        if (product is null) throw new NotFoundException("Product not found.");

        var allowedTypes = new[] { "image/jpeg", "image/png", "image/webp" };
        const long maxBytes = 5 * 1024 * 1024; // 5MB

        // Helper to validate+upload+delete old
        async Task UpdateImage(
            ImageFileInput? file,
            bool remove,
            string? oldUrl,
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

            if (file is null) return; // no change

            // validate
            if (!allowedTypes.Contains(file.ContentType, StringComparer.OrdinalIgnoreCase))
                throw new InvalidOperationException($"Invalid file type for image: {file.ContentType}");
            if (file.Content.Length > maxBytes)
                throw new InvalidOperationException("Image exceeds 5MB limit.");

            // delete old
            if (!string.IsNullOrWhiteSpace(oldPublicId))
                await _cloudinary.DeleteAsync(oldPublicId);

            // upload new
            var folder = $"ecommerce/products/{product.Id}";
            using var stream = file.Content;
            var (url, publicId) = await _cloudinary.UploadAsync(stream, file.FileName, file.ContentType, folder);
            setter(url, publicId);
        }

        await UpdateImage(input.Front, input.RemoveFront, product.FrontImageUrl, product.FrontImagePublicId, product.SetFrontImage);
        await UpdateImage(input.Back, input.RemoveBack, product.BackImageUrl, product.BackImagePublicId, product.SetBackImage);
        await UpdateImage(input.Side, input.RemoveSide, product.SideImageUrl, product.SideImagePublicId, product.SetSideImage);

        await _repository.UpdateAsync(product);
        await _repository.SaveChangesAsync();

        await _cache.RemoveAsync(CacheKeys.ProductDetails(id));

        return MapToResponse(product, null);
    }
}