namespace Ecommerce.Application.DTOs.Products;

public record ProductResponse(
    Guid Id,
    string Name,
    string Slug,                         
    string Description,
    decimal Price,
    decimal FinalPrice,
    bool HasDiscount,
    int StockQuantity,
    bool IsAvailable,
    string Category,
    string? FrontImageUrl,
    string? BackImageUrl,
    string? SideImageUrl
);