using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ecommerce.Application.DTOs.Cart;


public record CartItemResponse(
    Guid ProductId,
    string ProductName,
    decimal Price,
    int Quantity,
    decimal Total);

public record CartResponse(
    IEnumerable<CartItemResponse> Items,
    decimal TotalAmount);