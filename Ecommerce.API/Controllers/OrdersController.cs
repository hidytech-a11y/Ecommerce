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

    //Checkout from Cart (Option A)
    [HttpPost("checkout")]
    public async Task<IActionResult> Checkout()
    {
        var userId = Guid.Parse(
            User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        var order = await _orderService.CreateOrderFromCartAsync(userId);

        return Ok(ApiResponse<OrderResponse>
            .SuccessResponse(order, "Order created successfully"));
    }

    //Get all user orders
    [HttpGet]
    public async Task<IActionResult> GetOrders()
    {
        var userId = Guid.Parse(
            User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        var result = await _orderService.GetUserOrdersAsync(userId);

        return Ok(ApiResponse<IEnumerable<OrderResponse>>
            .SuccessResponse(result, "Orders fetched successfully"));
    }

    //Get single order
    [HttpGet("{id}")]
    public async Task<IActionResult> GetOrder(Guid id)
    {
        var userId = Guid.Parse(
            User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        var result = await _orderService.GetOrderAsync(id, userId);

        return Ok(ApiResponse<OrderResponse>
            .SuccessResponse(result, "Order fetched successfully"));
    }
}