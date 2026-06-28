using Ecommerce.Application.DTOs.Orders;
using FluentValidation;

namespace Ecommerce.Application.Validators.Orders;

public class CancelOrderRequestValidator : AbstractValidator<CancelOrderRequest>
{
    public CancelOrderRequestValidator()
    {
        RuleFor(x => x.Reason)
            .MaximumLength(500)
            .WithMessage("Cancellation reason must not exceed 500 characters.");
    }
}