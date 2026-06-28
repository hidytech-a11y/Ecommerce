using Ecommerce.Application.Common.Responses;
using Ecommerce.Application.DTOs.Orders;
using Ecommerce.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Ecommerce.Api.Controllers;

[ApiController]
[Route("api/v{version:apiVersion}/orders")]
[Authorize]
public class OrdersController : ControllerBase
{
    private readonly IOrderService _orderService;

    public OrdersController(IOrderService orderService)
    {
        _orderService = orderService;
    }

    [HttpPost("checkout")]
    public async Task<IActionResult> Checkout()
    {
        var userId = Guid.Parse(
            User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        var order = await _orderService.CreateOrderFromCartAsync(userId);

        return Ok(ApiResponse<OrderResponse>
            .SuccessResponse(order, "Order created successfully"));
    }

    [HttpGet]
    public async Task<IActionResult> GetOrders()
    {
        var userId = Guid.Parse(
            User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        var result = await _orderService.GetUserOrdersAsync(userId);

        return Ok(ApiResponse<IEnumerable<OrderResponse>>
            .SuccessResponse(result, "Orders fetched successfully"));
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetOrder(Guid id)
    {
        var userId = Guid.Parse(
            User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        var result = await _orderService.GetOrderAsync(id, userId);

        return Ok(ApiResponse<OrderResponse>
            .SuccessResponse(result, "Order fetched successfully"));
    }

    // User cancels their own order
    [HttpPost("{id}/cancel")]
    public async Task<IActionResult> CancelOrder(
        Guid id,
        [FromBody] CancelOrderRequest? request)
    {
        var userId = Guid.Parse(
            User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        var result = await _orderService.CancelOrderAsync(
            id,
            userId,
            request ?? new CancelOrderRequest());

        return Ok(ApiResponse<OrderResponse>
            .SuccessResponse(result, "Order cancelled successfully"));
    }
}