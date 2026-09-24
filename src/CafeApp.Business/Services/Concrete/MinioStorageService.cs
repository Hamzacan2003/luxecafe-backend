using CafeApp.Business.Services.Abstract;
using CafeApp.Business.Settings;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Minio;
using Minio.DataModel.Args;

namespace CafeApp.Business.Services.Concrete; 

public class MinioStorageService : IStorageService
{
    private readonly IMinioClient _minioClient;
    private readonly MinioSettings _settings;

    public MinioStorageService(IOptions<MinioSettings> settings)
    {
        _settings = settings.Value;

        var client = new MinioClient()
            .WithEndpoint(_settings.Endpoint)
            .WithCredentials(_settings.AccessKey, _settings.SecretKey);

        if (_settings.WithSSL)
            client = client.WithSSL();

        _minioClient = client.Build();
    }

    public async Task<string> UploadFileAsync(IFormFile file)
    {
        // 1. Bucket var mı kontrol et, yoksa oluştur
        var bucketExists = await _minioClient.BucketExistsAsync(
            new BucketExistsArgs().WithBucket(_settings.BucketName));

        if (!bucketExists)
        {
            await _minioClient.MakeBucketAsync(
                new MakeBucketArgs().WithBucket(_settings.BucketName));

            // Bucket'ı public okumaya açıyoruz (resimleri herkes görebilsin diye)
            var policy = $@"{{
                ""Version"": ""2012-10-17"",
                ""Statement"": [
                    {{
                        ""Effect"": ""Allow"",
                        ""Principal"": {{""AWS"": [""*""]}},
                        ""Action"": [""s3:GetObject""],
                        ""Resource"": [""arn:aws:s3:::{_settings.BucketName}/*""]
                    }}
                ]
            }}";

            await _minioClient.SetPolicyAsync(
                new SetPolicyArgs().WithBucket(_settings.BucketName).WithPolicy(policy));
        }

        // 2. Benzersiz dosya adı üret
        var extension = Path.GetExtension(file.FileName);
        var objectName = $"{Guid.NewGuid():N}{extension}";

        using var stream = file.OpenReadStream();

        // 3. MinIO'ya yükle
        await _minioClient.PutObjectAsync(new PutObjectArgs()
            .WithBucket(_settings.BucketName)
            .WithObject(objectName)
            .WithStreamData(stream)
            .WithObjectSize(file.Length)
            .WithContentType(file.ContentType));

        // 4. Doğrudan erişilebilir resim URL'ini döndür
        var protocol = _settings.WithSSL ? "https" : "http";
        return $"{protocol}://{_settings.Endpoint}/{_settings.BucketName}/{objectName}";
    }
}