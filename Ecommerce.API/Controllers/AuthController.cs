using Ecommerce.Application.DTOs.Auth;
using Ecommerce.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Ecommerce.Application.Common.Responses;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;

namespace Ecommerce.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register(RegisterRequest request)
    {
        var response = await _authService.RegisterAsync(request);
        return Ok(ApiResponse<AuthResponse>
        .SuccessResponse(response, "Authentication successful"));
    }

    [EnableRateLimiting("AuthPolicy")]
    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginRequest request)
    {
        var response = await _authService.LoginAsync(request);
        return Ok(ApiResponse<AuthResponse>
        .SuccessResponse(response, "Authentication successful"));
    }
}