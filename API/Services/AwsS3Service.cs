using Amazon.S3;
using Amazon.S3.Model;
using Amazon.S3.Util;
using API.Interfaces;
using Microsoft.Extensions.Options;
using System.Net;

namespace API.Services
{
    public class AwsS3Service : IObjectStorageService
    {
        private readonly IAmazonS3 _client;
        private readonly AwsS3Settings _settings;

        public AwsS3Service(IOptions<AwsS3Settings> options, IAmazonS3 amazonS3)
        {
            _settings = options.Value;
            _client = amazonS3;
        }

        public async Task<string> Upload(IFormFile file)
        {
            using var stream = file.OpenReadStream();

            var bucketExists = await BucketExists();
            if (!bucketExists)
                await CreateBucket();

            var key = Guid.NewGuid().ToString();
            var request = new PutObjectRequest
            {
                BucketName = _settings.BucketName,
                Key = key,
                InputStream = stream,
                ContentType = file.ContentType,
                Metadata =
                {
                    ["scan-status"] = "PENDING",
                    ["original-file-name"] = file.FileName
                }
            };

            await _client.PutObjectAsync(request);

            return key;
        }

        private async Task<bool> BucketExists()
        {
            return await AmazonS3Util.DoesS3BucketExistV2Async(_client, _settings.BucketName);
        }

        private async Task CreateBucket()
        {
            var request = new PutBucketRequest
            {
                BucketName = _settings.BucketName,
            };

            var response = await _client.PutBucketAsync(request);

            if (response.HttpStatusCode != HttpStatusCode.OK)
                throw new Exception($"Bucket couldnt be created");
        }
    }

    public class AwsS3Settings
    {
        public required string BucketName { get; set; }
        public required string Endpoint { get; set; }
        public required string AccessKey { get; set; }
        public required string SecretKey { get; set; }
        public required string Region { get; set; }
        public required bool WithSSL { get; set; }
    }

    public static class AwsS3ServiceExtensions
    {
        public static void AddAwsS3Settings(this IServiceCollection services, IConfiguration configuration)
        {
            services.Configure<AwsS3Settings>(configuration.GetSection("MinIO"));
        }

        public static void ConfigureAmazonS3(this IServiceCollection services)
        {
            services.AddSingleton<IAmazonS3>(sp =>
            {
                var settings = sp.GetRequiredService<IOptions<AwsS3Settings>>().Value;
                var config = new AmazonS3Config
                {
                    ServiceURL = $"http://{settings.Endpoint}",
                    ForcePathStyle = true
                };

                return new AmazonS3Client(settings.AccessKey, settings.SecretKey, config);
            });
        }

        public static void AddAwsS3Service(this IServiceCollection services)
        {
            services.AddSingleton<IObjectStorageService, AwsS3Service>();
        }
    }
}
