using Ecommerce.Application.Common.Responses;
using Ecommerce.Application.DTOs.Orders;
using Ecommerce.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Ecommerce.Api.Controllers;

[ApiController]
[Route("api/orders")]
[Authorize]
public class OrdersController : ControllerBase
{
    private readonly IOrderService _service;

    public OrdersController(IOrderService service)
    {
        _service = service;
    }

    [HttpPost]
    public async Task<IActionResult> CreateOrder(CreateOrderRequest request)
    {
        var userId = Guid.Parse(
            User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        var result = await _service.CreateOrderAsync(userId, request);

        return Ok(ApiResponse<OrderResponse>
            .SuccessResponse(result, "Order created"));
    }

    [HttpGet]
    public async Task<IActionResult> GetOrders()
    {
        var userId = Guid.Parse(
            User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        var result = await _service.GetUserOrdersAsync(userId);

        return Ok(ApiResponse<IEnumerable<OrderResponse>>
            .SuccessResponse(result));
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetOrder(Guid id)
    {
        var userId = Guid.Parse(
            User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        var result = await _service.GetOrderAsync(id, userId);

        return Ok(ApiResponse<OrderResponse>
            .SuccessResponse(result));
    }
}