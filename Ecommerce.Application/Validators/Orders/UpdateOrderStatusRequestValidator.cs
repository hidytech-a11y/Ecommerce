using Ecommerce.Application.DTOs.Orders;
using FluentValidation;

namespace Ecommerce.Application.Validators.Orders;

public class UpdateOrderStatusRequestValidator
    : AbstractValidator<UpdateOrderStatusRequest>
{
    public UpdateOrderStatusRequestValidator()
    {
        RuleFor(x => x.Status)
            .NotEmpty()
            .WithMessage("Status is required.")
            .Must(s =>
                s == "Pending" ||
                s == "Paid" ||
                s == "Cancelled")
            .WithMessage("Status must be one of: Pending, Paid, Cancelled.");
    }
}