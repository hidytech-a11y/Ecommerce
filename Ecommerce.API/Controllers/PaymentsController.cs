using Ecommerce.Application.Common.Responses;
using Ecommerce.Application.DTOs.Payments;
using Ecommerce.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System.Security.Claims;

namespace Ecommerce.Api.Controllers;

[ApiController]
[Route("api/payments")]
[Authorize]
public class PaymentsController : ControllerBase
{
    private readonly IPaymentService _service;

    public PaymentsController(IPaymentService service)
    {
        _service = service;
    }

    [EnableRateLimiting("PaymentPolicy")]
    [HttpPost("initialize")]
    public async Task<IActionResult> Initialize(
        InitializePaymentRequest request)
    {
        var userId = Guid.Parse(
            User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        var result = await _service.InitializePaymentAsync(userId, request);

        return Ok(ApiResponse<InitializePaymentResponse>
            .SuccessResponse(result));
    }

    [HttpPost("webhook")]
    [AllowAnonymous]
    public async Task<IActionResult> Webhook()
    {
        await _service.HandleWebhookAsync(Request);

        return Ok();
    }
}