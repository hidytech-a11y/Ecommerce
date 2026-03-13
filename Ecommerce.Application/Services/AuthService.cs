using Ecommerce.Application.Common.Security;
using Ecommerce.Application.DTOs.Auth;
using Ecommerce.Application.Interfaces;
using Ecommerce.Domain.Entities;
using Ecommerce.Domain.Enums;
using Ecommerce.Application.Common.Exceptions;

namespace Ecommerce.Application.Services;

public class AuthService : IAuthService
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenGenerator _jwt;

    public AuthService(
        IUserRepository userRepository,
        IPasswordHasher passwordHasher,
        IJwtTokenGenerator jwt)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
        _jwt = jwt;
    }

    public async Task<AuthResponse> RegisterAsync(RegisterRequest request)
    {
        if (await _userRepository.ExistsByEmailAsync(request.Email))
            throw new BadRequestException("Email already exists.");

        var hashedPassword = _passwordHasher.Hash(request.Password);

        var user = new User(
            request.Email,
            hashedPassword,
            UserRole.Customer);

        await _userRepository.AddAsync(user);
        await _userRepository.SaveChangesAsync();

        var token = _jwt.GenerateToken(user);

        return new AuthResponse(
            token,
            DateTime.UtcNow.AddMinutes(60),
            user.Email,
            user.Role.ToString());
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request)
    {
        var user = await _userRepository.GetByEmailAsync(request.Email);

        if (user is null)
            throw new UnauthorizedException("Invalid credentials.");

        var isValid = _passwordHasher.Verify(
            user.PasswordHash,
            request.Password);

        if (!isValid)
            throw new UnauthorizedException("Invalid credentials.");

        var token = _jwt.GenerateToken(user);

        return new AuthResponse(
            token,
            DateTime.UtcNow.AddMinutes(60),
            user.Email,
            user.Role.ToString());
    }
}