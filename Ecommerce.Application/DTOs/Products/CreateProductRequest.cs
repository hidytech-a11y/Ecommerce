using Microsoft.AspNetCore.Http;

namespace Ecommerce.Application.DTOs.Products;

public class CreateProductRequest
{
    public string Name { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public decimal Price { get; set; }

    public int StockQuantity { get; set; }

    public Guid CategoryId { get; set; }

    public bool IsAvailable { get; set; }

    public IFormFile? FrontImage { get; set; }

    public IFormFile? BackImage { get; set; }

    public IFormFile? SideImage { get; set; }
}