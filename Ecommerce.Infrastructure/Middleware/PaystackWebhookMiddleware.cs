using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Security.Cryptography;
using System.Text;

namespace Ecommerce.Infrastructure.Middleware;

public class PaystackWebhookMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IConfiguration _config;
    private readonly ILogger<PaystackWebhookMiddleware> _logger;

    public PaystackWebhookMiddleware(
        RequestDelegate next,
        IConfiguration config,
        ILogger<PaystackWebhookMiddleware> logger)
    {
        _next = next;
        _config = config;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (!context.Request.Path.StartsWithSegments("/api/payments/webhook"))
        {
            await _next(context);
            return;
        }

        _logger.LogInformation(
            "Paystack webhook request received. IP={IP}",
            context.Connection.RemoteIpAddress);

        var secret = _config["Paystack:SecretKey"];

        context.Request.EnableBuffering();

        using var reader = new StreamReader(
            context.Request.Body,
            Encoding.UTF8,
            leaveOpen: true);

        var body = await reader.ReadToEndAsync();

        context.Request.Body.Position = 0;

        var signature = context.Request.Headers["x-paystack-signature"];

        if (string.IsNullOrEmpty(signature))
        {
            _logger.LogWarning(
                "Webhook rejected. Missing Paystack signature header. IP={IP}",
                context.Connection.RemoteIpAddress);

            context.Response.StatusCode = 401;
            await context.Response.WriteAsync("Missing Paystack signature");
            return;
        }

        using var hmac = new HMACSHA512(Encoding.UTF8.GetBytes(secret));

        var computedHash = BitConverter
            .ToString(hmac.ComputeHash(Encoding.UTF8.GetBytes(body)))
            .Replace("-", "")
            .ToLower();

        if (signature != computedHash)
        {
            _logger.LogWarning(
                "Invalid Paystack webhook signature detected. IP={IP}",
                context.Connection.RemoteIpAddress);

            context.Response.StatusCode = 401;
            await context.Response.WriteAsync("Invalid Paystack signature");
            return;
        }

        _logger.LogInformation(
            "Paystack webhook signature verified successfully.");

        await _next(context);
    }
}