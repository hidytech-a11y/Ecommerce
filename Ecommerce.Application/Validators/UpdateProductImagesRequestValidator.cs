using Ecommerce.Application.DTOs.Products;
using FluentValidation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ecommerce.Application.Validators;

public class UpdateProductImagesRequestValidator : AbstractValidator<UpdateProductImagesRequest>
{
    private static readonly string[] Allowed = { "image/jpeg", "image/png", "image/webp" };
    public UpdateProductImagesRequestValidator()
    {
        RuleFor(x => x.FrontImage)
            .Must(f => f == null || Allowed.Contains(f.ContentType))
            .WithMessage("Front image must be jpeg/png/webp.")
            .Must(f => f == null || f.Length <= 5 * 1024 * 1024)
            .WithMessage("Front image must be ≤ 5MB.");

       

        RuleFor(x => x)
            .Must(x => !(x.RemoveFront && x.FrontImage != null))
            .WithMessage("Cannot provide a front image and request removal at the same time.");
        
    }
}
