using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ecommerce.Application.DTOs.Products;

public class UpdateProductImagesRequest
{
    public IFormFile? FrontImage { get; set; }
    public IFormFile? BackImage { get; set; }
    public IFormFile? SideImage { get; set; }

    public bool RemoveFront { get; set; }
    public bool RemoveBack { get; set; }
    public bool RemoveSide { get; set; }
}


public record ImageFileInput(string FileName, string ContentType, Stream Content);

public record UpdateProductImagesInput(
    ImageFileInput? Front,
    ImageFileInput? Back,
    ImageFileInput? Side,
    bool RemoveFront,
    bool RemoveBack,
    bool RemoveSide
);
