using Ecommerce.Domain.Entities;

namespace Ecommerce.Application.Interfaces;

public interface IJwtTokenGenerator
{
    string GenerateToken(User user);
}
