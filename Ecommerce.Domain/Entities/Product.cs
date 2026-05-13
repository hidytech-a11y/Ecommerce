using Ecommerce.Domain.Common;

namespace Ecommerce.Domain.Entities;

public class Product : BaseEntity
{
    public string Name { get; private set; } = default!;
    public string Description { get; private set; } = default!;
    public decimal Price { get; private set; }
    public int StockQuantity { get; private set; }
    public bool IsAvailable { get; private set; }
    public Guid CategoryId { get; private set; }
    public byte[]? RowVersion { get; private set; }

    
    public string? FrontImageUrl { get; private set; }
    public string? BackImageUrl { get; private set; }
    public string? SideImageUrl { get; private set; }

    public string? FrontImagePublicId { get; private set; }
    public string? BackImagePublicId { get; private set; }
    public string? SideImagePublicId { get; private set; }

    private Product() { }

    public Product(string name, string description, decimal price, int stock, Guid categoryId)
    {
        Name = name;
        Description = description;
        Price = price;
        StockQuantity = stock;
        CategoryId = categoryId;
        IsAvailable = true;
    }

    public void ReduceStock(int quantity)
    {
        if (quantity <= 0)
            throw new InvalidOperationException("Invalid quantity.");

        if (StockQuantity < quantity)
            throw new InvalidOperationException("Insufficient stock.");

        StockQuantity -= quantity;
    }

    public void Update(string name, string description, decimal price, int stock, bool isAvailable)
    {
        Name = name;
        Description = description;
        Price = price;
        StockQuantity = stock;
        IsAvailable = isAvailable;
    }

   
    public void SetFrontImage(string? url, string? publicId)
    {
        FrontImageUrl = url;
        FrontImagePublicId = publicId;
    }

    public void SetBackImage(string? url, string? publicId)
    {
        BackImageUrl = url;
        BackImagePublicId = publicId;
    }

    public void SetSideImage(string? url, string? publicId)
    {
        SideImageUrl = url;
        SideImagePublicId = publicId;
    }
}