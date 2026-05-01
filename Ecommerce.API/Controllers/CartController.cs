using Ecommerce.Application.Interfaces;
using Ecommerce.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

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


    private Guid GetUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrEmpty(userIdClaim))
        {
            throw new UnauthorizedAccessException("Invalid token: user id missing");
        }

        return Guid.Parse(userIdClaim);
    }

[HttpPost("add")]
    public async Task<IActionResult> Add(Guid productId, int quantity)
    {
        var userId = GetUserId();

        await _cartService.AddToCartAsync(userId, productId, quantity);

        return Ok("Added to cart");
    }

    [HttpGet]
    public async Task<IActionResult> Get()
    {
        var userId = GetUserId();

        var cart = await _cartService.GetCartAsync(userId);

        return Ok(cart);
    }

    [HttpDelete("remove")]
    public async Task<IActionResult> Remove(Guid productId)
    {
        var userId = GetUserId();

        await _cartService.RemoveFromCartAsync(userId, productId);

        return Ok();
    }

    [HttpDelete("clear")]
    public async Task<IActionResult> Clear()
    {
        var userId = GetUserId();

        await _cartService.ClearCartAsync(userId);

        return Ok();
    }
}