using Ecommerce.Application.Common.Security;
using Ecommerce.Domain.Entities;
using Microsoft.AspNetCore.Identity;

namespace Ecommerce.Infrastructure.Security;

public class PasswordHasherAdapter : IPasswordHasher
{
    private readonly IPasswordHasher<User> _hasher;

    public PasswordHasherAdapter()
    {
        _hasher = new PasswordHasher<User>();
    }

    public string Hash(string password)
    {
        return _hasher.HashPassword(null!, password);
    }

    public bool Verify(string hashedPassword, string providedPassword)
    {
        var result = _hasher.VerifyHashedPassword(
            null!,
            hashedPassword,
            providedPassword);

        return result != PasswordVerificationResult.Failed;
    }
}