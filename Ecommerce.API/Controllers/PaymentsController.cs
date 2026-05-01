using Ecommerce.Application.DTOs.Payments;
using Ecommerce.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

[ApiController]
[Route("api/v{version:apiVersion}/payments")]
public class PaymentsController : ControllerBase
{
    private readonly IPaymentService _paymentService;
    private readonly IConfiguration _configuration;
    private readonly ILogger<PaymentsController> _logger;

    public PaymentsController(
        IPaymentService paymentService,
        IConfiguration configuration,
        ILogger<PaymentsController> logger)
    {
        _paymentService = paymentService;
        _configuration = configuration;
        _logger = logger;
    }

    //USER-BASED ENDPOINT (requires JWT)
    [Authorize]
    [HttpPost("initialize")]
    public async Task<IActionResult> Initialize([FromBody] InitializePaymentRequest request)
    {
        var userId = GetUserId();

        var result = await _paymentService.InitializePaymentAsync(userId, request);

        return Ok(result);
    }

    //WEBHOOK (NO AUTH)
    [AllowAnonymous]
    [HttpPost("webhook")]
    public async Task<IActionResult> HandleWebhook()
    {
        _logger.LogInformation("Paystack webhook hit");

        //Read request body
        Request.EnableBuffering();

        using var reader = new StreamReader(Request.Body, Encoding.UTF8, leaveOpen: true);
        var body = await reader.ReadToEndAsync();

        Request.Body.Position = 0;

        //Get signature from header
        var signature = Request.Headers["x-paystack-signature"].FirstOrDefault();

        if (string.IsNullOrEmpty(signature))
        {
            _logger.LogWarning("Missing Paystack signature");
            return Unauthorized("Missing Paystack signature");
        }

        //Compute hash
        var secretKey = _configuration["Paystack:SecretKey"];

        using var hmac = new HMACSHA512(Encoding.UTF8.GetBytes(secretKey));
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(body));
        var computedSignature = BitConverter.ToString(hash).Replace("-", "").ToLower();

        //Compare signatures
        if (computedSignature != signature)
        {
            _logger.LogWarning("Invalid Paystack signature");
            return Unauthorized("Invalid signature");
        }

        //Process webhook
        await _paymentService.HandleWebhookAsync(Request);

        _logger.LogInformation("Webhook processed successfully");

        return Ok();
    }

    // Helper method
    private Guid GetUserId()
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrEmpty(userId))
        {
            throw new UnauthorizedAccessException("Invalid token");
        }

        return Guid.Parse(userId);
    }
}