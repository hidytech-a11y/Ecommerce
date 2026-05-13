using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Ecommerce.Application.Interfaces;
using Microsoft.Extensions.Configuration;
using Npgsql.BackendMessages;

namespace Ecommerce.Infrastructure.Cloudinary;

public class CloudinaryService : ICloudinaryService
{
    private readonly CloudinaryDotNet.Cloudinary _cloudinary;
    private readonly string _folderBase;

    public CloudinaryService(IConfiguration configuration)
    {
        var cloudName = configuration["Cloudinary:CloudName"] ?? throw new InvalidOperationException("Cloudinary:CloudName missing");
        var apiKey = configuration["Cloudinary:ApiKey"] ?? throw new InvalidOperationException("Cloudinary:ApiKey missing");
        var apiSecret = configuration["Cloudinary:ApiSecret"] ?? throw new InvalidOperationException("Cloudinary:ApiSecret missing");
        _folderBase = configuration["Cloudinary:Folder"] ?? "ecommerce/products";

        var account = new Account(cloudName, apiKey, apiSecret);
        _cloudinary = new CloudinaryDotNet.Cloudinary(account);
    }

    public async Task<(string Url, string PublicId)> UploadAsync(Stream fileStream, string fileName, string contentType, string folder)
    {
        var uploadParams = new ImageUploadParams
        {
            File = new FileDescription(fileName, fileStream),
            Folder = folder,
            UseFilename = true,
            UniqueFilename = true,
            Overwrite = false
        };

        var result = await _cloudinary.UploadAsync(uploadParams);
        if (result.Error != null)
            throw new InvalidOperationException($"Cloudinary upload failed: {result.Error.Message}");

        return (result.SecureUrl?.ToString() ?? result.Url?.ToString() ?? string.Empty, result.PublicId);
    }

    public async Task DeleteAsync(string publicId)
    {
        if (string.IsNullOrWhiteSpace(publicId)) return;
        var delParams = new DeletionParams(publicId);
        await _cloudinary.DestroyAsync(delParams);
    }
}