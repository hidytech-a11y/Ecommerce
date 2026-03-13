using Ecommerce.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ecommerce.Application.DTOs.Products;

public record CreateDiscountRequest(
    Guid ProductId,
    DiscountType DiscountType,
    decimal Value,
    DateTime StartDate,
    DateTime EndDate
);
