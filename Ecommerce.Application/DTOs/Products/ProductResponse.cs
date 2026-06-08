namespace Ecommerce.Application.DTOs.Products;

public record ProductResponse(
    Guid Id,
    string Name,
    string Description,
    decimal OriginalPrice,
    decimal FinalPrice,
    bool DiscountApplied,
    int StockQuantity,
    bool IsAvailable,
    string? FrontImageUrl,
    string? BackImageUrl,
    string? SideImageUrl,
    string Category
);