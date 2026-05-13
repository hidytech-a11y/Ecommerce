using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ecommerce.Application.Interfaces;

public interface ICloudinaryService
{
    Task<(string Url, string PublicId)> 
        UploadAsync(Stream fileStream, 
        string fileName, 
        string contentType, 
        string folder
        
        );
    Task DeleteAsync(string publicId);
}