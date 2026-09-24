using CafeApp.Business.Services.Abstract;
using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace CafeApp.Business.Services.Concrete;

public class MinioStorageService : IStorageService
{
    private readonly Cloudinary _cloudinary;
    private readonly ILogger<MinioStorageService> _logger;
    private const string DefaultFallbackImage = "https://images.unsplash.com/photo-1517248135467-4c7edcad34c4?w=600";

    public MinioStorageService(IConfiguration configuration, ILogger<MinioStorageService> logger)
    {
        _logger = logger;

        var cloudName = configuration["Cloudinary:CloudName"];
        var apiKey = configuration["Cloudinary:ApiKey"];
        var apiSecret = configuration["Cloudinary:ApiSecret"];

        if (!string.IsNullOrEmpty(cloudName) && !string.IsNullOrEmpty(apiKey) && !string.IsNullOrEmpty(apiSecret))
        {
            var account = new Account(cloudName, apiKey, apiSecret);
            _cloudinary = new Cloudinary(account);
            _cloudinary.Api.Secure = true;
        }
    }

    public async Task<string> UploadFileAsync(IFormFile file)
    {
        if (file == null || file.Length == 0)
            return DefaultFallbackImage;

        if (_cloudinary == null)
        {
            _logger.LogWarning("Cloudinary yapılandırılmamış, varsayılan görsel kullanılıyor.");
            return DefaultFallbackImage;
        }

        try
        {
            await using var stream = file.OpenReadStream();
            var uploadParams = new ImageUploadParams
            {
                File = new FileDescription(file.FileName, stream),
                Folder = "luxecafe_products"
            };

            var uploadResult = await _cloudinary.UploadAsync(uploadParams);
            return uploadResult.SecureUrl?.ToString() ?? DefaultFallbackImage;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Görsel Cloudinary'ye yüklenirken hata oluştu.");
            return DefaultFallbackImage;
        }
    }
}