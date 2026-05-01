using Asp.Versioning;
using Ecommerce.Application.Common.Pagination;
using Ecommerce.Application.Common.Responses;
using Ecommerce.Application.DTOs.Products;
using Ecommerce.Application.Interfaces;
using Ecommerce.Domain.Entities;
using Ecommerce.Infrastructure.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Ecommerce.Api.Controllers;


[EnableRateLimiting("ApiPolicy")]
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/products")]
public class ProductsController : ControllerBase
{
    private readonly IProductService _service;
    private readonly IDiscountRepository _discountRepository;


    public ProductsController(IProductService service, IDiscountRepository discountRepository)
    {
        _service = service;
        _discountRepository = discountRepository;
    }

    [HttpGet]
    public async Task<IActionResult> GetProducts([FromQuery] ProductQueryParameters query)
    {
        var result = await _service.GetProductsAsync(query);

        return Ok(ApiResponse<PagedResult<ProductResponse>>
            .SuccessResponse(result));
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetProduct(Guid id)
    {
        var product = await _service.GetProductAsync(id);

        return Ok(ApiResponse<ProductResponse>
            .SuccessResponse(product));
    }

    [Authorize(Roles = "Admin")]
    [HttpPost]
    public async Task<IActionResult> CreateProduct(CreateProductRequest request)
    {
        var product = await _service.CreateProductAsync(request);

        return Ok(ApiResponse<ProductResponse>
            .SuccessResponse(product, "Product created"));
    }

    [Authorize(Roles = "Admin")]
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateProduct(Guid id, UpdateProductRequest request)
    {
        var product = await _service.UpdateProductAsync(id, request);

        return Ok(ApiResponse<ProductResponse>
            .SuccessResponse(product, "Product updated"));
    }

    [Authorize(Roles = "Admin")]
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteProduct(Guid id)
    {
        await _service.DeleteProductAsync(id);

        return Ok(ApiResponse<object>
            .SuccessResponse(null, "Product deleted"));
    }

    [Authorize(Roles = "Admin")]
    [HttpPost("discounts")]
    public async Task<IActionResult> CreateDiscount(CreateDiscountRequest request)
    {
        var discount = new Discount(
            request.ProductId,
            request.DiscountType,
            request.Value,
            request.StartDate,
            request.EndDate);

        await _discountRepository.AddAsync(discount);
        await _discountRepository.SaveChangesAsync();

        return Ok(ApiResponse<object>
            .SuccessResponse(null, "Discount created"));
    }
}