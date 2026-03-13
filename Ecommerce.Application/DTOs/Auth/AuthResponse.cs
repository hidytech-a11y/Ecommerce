using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ecommerce.Application.DTOs.Auth;

public record AuthResponse(
    string AccessToken,
    DateTime ExpiresAt,
    string Email,
    string Role
);