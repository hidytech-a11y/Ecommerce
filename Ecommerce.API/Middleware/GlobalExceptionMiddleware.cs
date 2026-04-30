using Ecommerce.Application.Common.Errors;
using Ecommerce.Application.Common.Exceptions;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

public class GlobalExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionMiddleware> _logger;

    public GlobalExceptionMiddleware(
        RequestDelegate next,
        ILogger<GlobalExceptionMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await HandleException(context, ex);
        }
    }

    private async Task HandleException(
        HttpContext context,
        Exception ex)
    {
        var traceId = context.TraceIdentifier;

        ProblemDetails problem;

        switch (ex)
        {
            case NotFoundException nf:
                context.Response.StatusCode = StatusCodes.Status404NotFound;

                problem = new ProblemDetails
                {
                    Type = $"https://api.ecommerce.com/errors/{nf.ErrorCode.ToLower()}",
                    Title = "Resource not found",
                    Status = 404,
                    Detail = nf.Message,
                    Instance = context.Request.Path,
                    Extensions =
                    {
                        ["errorCode"] = nf.ErrorCode,
                        ["traceId"] = traceId
                    }
                };
                break;

            case UnauthorizedException:
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;

                problem = new ProblemDetails
                {
                    Title = "Unauthorized",
                    Status = 401,
                    Detail = ex.Message,
                    Instance = context.Request.Path,
                    Extensions =
                    {
                        ["errorCode"] = ErrorCodes.Unauthorized,
                        ["traceId"] = traceId
                    }
                };
                break;

            case BadRequestException:
                context.Response.StatusCode = StatusCodes.Status400BadRequest;

                problem = new ProblemDetails
                {
                    Title = "Bad Request",
                    Status = 400,
                    Detail = ex.Message,
                    Instance = context.Request.Path,
                    Extensions =
                    {
                        ["errorCode"] = ErrorCodes.ValidationFailed,
                        ["traceId"] = traceId
                    }
                };
                break;

            default:
                context.Response.StatusCode = StatusCodes.Status500InternalServerError;

                problem = new ProblemDetails
                {
                    Title = "Internal Server Error",
                    Status = 500,
                    Detail = ex.ToString(),
                    Instance = context.Request.Path,
                    Extensions =
                    {
                        ["errorCode"] = "INTERNAL_ERROR",
                        ["traceId"] = traceId
                    }
                };

                _logger.LogError(ex, "Unhandled exception occurred");
                break;
        }

        context.Response.ContentType = "application/json";

        await context.Response.WriteAsync(
            JsonSerializer.Serialize(problem));
    }
}