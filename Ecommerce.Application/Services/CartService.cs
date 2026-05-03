using Ecommerce.Application.Common.Exceptions;
using Ecommerce.Application.DTOs.Cart;
using Ecommerce.Application.Interfaces;
using Ecommerce.Domain.Entities;

public class CartService : ICartService
{
    private readonly ICartRepository _cartRepo;
    private readonly IProductRepository _productRepo;

    public CartService(
        ICartRepository cartRepo,
        IProductRepository productRepo)
    {
        _cartRepo = cartRepo;
        _productRepo = productRepo;
    }

    public async Task AddToCartAsync(Guid userId, Guid productId, int quantity)
    {
        if (quantity <= 0)
            throw new BadRequestException("Quantity must be greater than zero.");

        var product = await _productRepo.GetByIdAsync(productId);

        if (product is null)
            throw new NotFoundException("Product not found.");

        await _cartRepo.AddItemAsync(userId, productId, quantity);
    }

    public async Task RemoveFromCartAsync(Guid userId, Guid productId)
    {
        var cart = await _cartRepo.GetByUserIdAsync(userId);

        if (cart is null)
            return;

        await _cartRepo.RemoveItemAsync(userId, productId);
    }

    public async Task<CartResponse> GetCartAsync(Guid userId)
    {
        var cart = await _cartRepo.GetByUserIdAsync(userId);

        if (cart is null)
            return new CartResponse([], 0);

        var items = new List<CartItemResponse>();
        decimal total = 0;

        foreach (var item in cart.Items)
        {
            var product = await _productRepo.GetByIdAsync(item.ProductId);

            if (product is null) continue;

            var itemTotal = product.Price * item.Quantity;

            total += itemTotal;

            items.Add(new CartItemResponse(
                product.Id,
                product.Name,
                product.Price,
                item.Quantity,
                itemTotal));
        }

        return new CartResponse(items, total);
    }

    public async Task ClearCartAsync(Guid userId)
    {
        var cart = await _cartRepo.GetByUserIdAsync(userId);

        if (cart is null) return;

        await _cartRepo.ClearAsync(userId);
    }
}
