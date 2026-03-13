using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ecommerce.Application.DTOs.Auth;

public record RegisterRequest(
    string Email,
    string Password
);
