namespace Ecommerce.Application.DTOs.Products;

public record UpdateProductRequest(
    string Name,
    string Description,
    decimal Price,
    int StockQuantity,
    Guid CategoryId,
    bool IsAvailable
);