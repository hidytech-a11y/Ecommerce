using Ecommerce.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[Authorize]
[ApiController]
[Route("api/v{version:apiVersion}/cart")]
public class CartController : ControllerBase
{
    private readonly ICartService _cartService;

    public CartController(ICartService cartService)
    {
        _cartService = cartService;
    }

    [HttpPost("add")]
    public async Task<IActionResult> Add(Guid productId, int quantity)
    {
        var userId = Guid.Parse(User.FindFirst("id")!.Value);

        await _cartService.AddToCartAsync(userId, productId, quantity);

        return Ok("Added to cart");
    }

    [HttpGet]
    public async Task<IActionResult> Get()
    {
        var userId = Guid.Parse(User.FindFirst("id")!.Value);

        var cart = await _cartService.GetCartAsync(userId);

        return Ok(cart);
    }

    [HttpDelete("remove")]
    public async Task<IActionResult> Remove(Guid productId)
    {
        var userId = Guid.Parse(User.FindFirst("id")!.Value);

        await _cartService.RemoveFromCartAsync(userId, productId);

        return Ok();
    }

    [HttpDelete("clear")]
    public async Task<IActionResult> Clear()
    {
        var userId = Guid.Parse(User.FindFirst("id")!.Value);

        await _cartService.ClearCartAsync(userId);

        return Ok();
    }
}