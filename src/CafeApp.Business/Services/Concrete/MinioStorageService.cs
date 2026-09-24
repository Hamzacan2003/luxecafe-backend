using CafeApp.Business.Services.Abstract;
using CafeApp.Business.Settings;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Minio;
using Minio.DataModel.Args;

namespace CafeApp.Business.Services.Concrete;

public class MinioStorageService : IStorageService
{
    private readonly IMinioClient _minioClient;
    private readonly MinioSettings _settings;
    private readonly ILogger<MinioStorageService> _logger;
    private const string DefaultFallbackImage = "https://images.unsplash.com/photo-1517248135467-4c7edcad34c4?w=600";

    public MinioStorageService(IOptions<MinioSettings> settings, ILogger<MinioStorageService> logger)
    {
        _settings = settings.Value;
        _logger = logger;

        try
        {
            var client = new MinioClient()
                .WithEndpoint(_settings.Endpoint)
                .WithCredentials(_settings.AccessKey, _settings.SecretKey);

            if (_settings.WithSSL)
                client = client.WithSSL();

            _minioClient = client.Build();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "MinIO Client başlatılamadı.");
        }
    }

    public async Task<string> UploadFileAsync(IFormFile file)
    {
        if (file == null || file.Length == 0)
            return DefaultFallbackImage;

        try
        {
            var extension = Path.GetExtension(file.FileName);
            var objectName = $"{Guid.NewGuid():N}{extension}";

            using var stream = file.OpenReadStream();

            // Dosyayı doğrudan yükle
            await _minioClient.PutObjectAsync(new PutObjectArgs()
                .WithBucket(_settings.BucketName)
                .WithObject(objectName)
                .WithStreamData(stream)
                .WithObjectSize(file.Length)
                .WithContentType(file.ContentType));

            var protocol = _settings.WithSSL ? "https" : "http";
            return $"{protocol}://{_settings.Endpoint}/{_settings.BucketName}/{objectName}";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Dosya yüklenirken hata oluştu. Varsayılan resme dönülüyor.");
            return DefaultFallbackImage;
        }
    }
}