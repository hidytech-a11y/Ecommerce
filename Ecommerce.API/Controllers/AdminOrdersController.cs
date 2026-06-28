using Asp.Versioning;
using Ecommerce.Application.Common.Pagination;
using Ecommerce.Application.Common.Responses;
using Ecommerce.Application.DTOs.Orders;
using Ecommerce.Application.Interfaces;
using Ecommerce.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System.Security.Claims;

namespace Ecommerce.Api.Controllers;

[EnableRateLimiting("ApiPolicy")]
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/admin/orders")]
[Authorize(Roles = "Admin")]
public class AdminOrdersController : ControllerBase
{
    private readonly IAdminOrderService _adminOrderService;
    private readonly IOrderService _orderService;

    public AdminOrdersController(IAdminOrderService adminOrderService, IOrderService orderService)
    {
        _adminOrderService = adminOrderService;
        _orderService = orderService;
    }

    /// Gets ALL orders across all users (Admin only).
    /// Supports filtering and pagination.
    [HttpGet]
    public async Task<IActionResult> GetAllOrders(
        [FromQuery] OrderQueryParameters query)
    {
        var result = await _adminOrderService.GetAllOrdersAsync(query);

        return Ok(ApiResponse<PagedResult<AdminOrderResponse>>
            .SuccessResponse(result, "Orders fetched successfully"));
    }

    /// Gets a single order by ID with full customer and items info (Admin only).
    [HttpGet("{id}")]
    public async Task<IActionResult> GetOrder(Guid id)
    {
        var result = await _adminOrderService.GetOrderByIdAsync(id);

        return Ok(ApiResponse<AdminOrderResponse>
            .SuccessResponse(result, "Order fetched successfully"));
    }

    /// Updates the status of an order (Admin only).
    /// Valid statuses: Pending, Paid, Cancelled.
    [HttpPatch("{id}/status")]
    public async Task<IActionResult> UpdateStatus(
        Guid id,
        [FromBody] UpdateOrderStatusRequest request)
    {
        var result = await _adminOrderService
            .UpdateOrderStatusAsync(id, request);

        return Ok(ApiResponse<AdminOrderResponse>
            .SuccessResponse(result, "Order status updated successfully"));
    }

    /// Gets dashboard statistics — counts, revenue, status breakdown, top products.
    [HttpGet("stats")]
    public async Task<IActionResult> GetStats()
    {
        var result = await _adminOrderService.GetStatsAsync();

        return Ok(ApiResponse<AdminOrderStatsResponse>
            .SuccessResponse(result, "Stats fetched successfully"));
    }

    // NEW: Admin cancels any order with full stock restoration + outbox event
    [HttpPost("{id}/cancel")]
    public async Task<IActionResult> CancelOrder(
        Guid id,
        [FromBody] CancelOrderRequest? request)
    {
        var adminId = Guid.Parse(
            User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        var result = await _orderService.AdminCancelOrderAsync(
            id,
            adminId,
            request ?? new CancelOrderRequest());

        return Ok(ApiResponse<OrderResponse>
            .SuccessResponse(result, "Order cancelled successfully"));
    }
}