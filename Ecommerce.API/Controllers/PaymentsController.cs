using Ecommerce.Application.DTOs.Payments;
using Ecommerce.Application.DTOs.Webhook;
using Ecommerce.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

[ApiController]
[Route("api/v{version:apiVersion}/payments")]
public class PaymentsController : ControllerBase
{
    private readonly IPaymentService _paymentService;
    private readonly IConfiguration _config;
    private readonly ILogger<PaymentsController> _logger;

    public PaymentsController(
        IPaymentService paymentService,
        IConfiguration config,
        ILogger<PaymentsController> logger)
    {
        _paymentService = paymentService;
        _config = config;
        _logger = logger;
    }

   
    [Authorize]
    [HttpPost("initialize")]
    public async Task<IActionResult> Initialize([FromBody] InitializePaymentRequest request)
    {
        foreach (var claim in User.Claims)
        {
            _logger.LogInformation("CLAIM: {Type} = {Value}", claim.Type, claim.Value);
        }

        var userId = GetUserId();

        var response = await _paymentService.InitializePaymentAsync(userId, request);

        return Ok(response);
    }

    
    [Authorize]
    [HttpGet("verify/{reference}")]
    public async Task<IActionResult> Verify(string reference)
    {
        await _paymentService.VerifyPaymentAsync(reference);

        return Ok(new { message = "Payment verified successfully" });
    }

    
    [AllowAnonymous]
    [HttpPost("webhook")]
    public async Task<IActionResult> Webhook()
    {
        try
        {
            var signature = Request.Headers["x-paystack-signature"].FirstOrDefault();

            if (string.IsNullOrEmpty(signature))
            {
                _logger.LogWarning("Missing Paystack signature");
                return Unauthorized("Missing Paystack signature");
            }

            // Read raw body
            Request.EnableBuffering();

            using var reader = new StreamReader(Request.Body, Encoding.UTF8, leaveOpen: true);
            var body = await reader.ReadToEndAsync();
            Request.Body.Position = 0;

            // Validate signature
            var secret = _config["Paystack:SecretKey"];

            using var hmac = new HMACSHA512(Encoding.UTF8.GetBytes(secret));
            var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(body));
            var computedSignature = BitConverter
                .ToString(hash)
                .Replace("-", "")
                .ToLower();

            if (computedSignature != signature)
            {
                _logger.LogWarning(" Invalid Paystack signature");
                return Unauthorized("Invalid Paystack signature");
            }

            _logger.LogInformation("Paystack webhook verified");

            var payload = JsonSerializer.Deserialize<PaystackWebhookDto>(body);

            if (payload == null)
            {
                _logger.LogWarning("Invalid webhook payload");
                return BadRequest();
            }

            if (payload.Event == "charge.success")
            {
                var reference = payload.Data.Reference;

                _logger.LogInformation("Payment success webhook: {Reference}", reference);

                await _paymentService.VerifyPaymentAsync(reference);
            }

            return Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Webhook failed");
            return StatusCode(500);
        }
    }


    private Guid GetUserId()
    {
        var userId =
            User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
            ?? User.FindFirst("sub")?.Value;

        if (string.IsNullOrEmpty(userId))
            throw new UnauthorizedAccessException("Invalid token: user id missing");

        return Guid.Parse(userId);
    }
}