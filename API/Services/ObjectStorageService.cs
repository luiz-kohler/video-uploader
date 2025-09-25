using Microsoft.Extensions.Options;
using Minio;
using Minio.DataModel.Args;

namespace API.Services
{
    public interface IObjectStorageService
    {
        Task<string> Upload(IFormFile file);
    }

    public class MinIOService : IObjectStorageService
    {
        private readonly IMinioClient _minioClient;
        private readonly MinIOVariables _minioConfig;

        public MinIOService(IMinioClientFactory minioClientFactory, IOptions<MinIOVariables> minioOptions)
        {
            _minioClient = minioClientFactory.CreateClient();
            _minioConfig = minioOptions.Value;
        }

        public async Task<string> Upload(IFormFile file)
        {
            try
            {
                var args = new BucketExistsArgs().WithBucket(_minioConfig.BucketName);
                var bucketExists = await _minioClient.BucketExistsAsync(args);

                if (!bucketExists)
                    await _minioClient.MakeBucketAsync(new MakeBucketArgs().WithBucket(_minioConfig.BucketName));

                var uniqueName = Guid.NewGuid().ToString();

                using var stream = file.OpenReadStream();
                var response = await _minioClient.PutObjectAsync(
                    new PutObjectArgs()
                    .WithBucket(_minioConfig.BucketName)
                    .WithObject(uniqueName)
                    .WithStreamData(stream)
                    .WithObjectSize(file.Length)
                    .WithContentType(file.ContentType)
                );

                return response.ObjectName;
            }
            catch
            {
                return string.Empty;
            }
        }
    }

    public class MinIOVariables
    {
        public required string BucketName { get; set; }
        public required string Endpoint { get; set; }
        public required string AccessKey { get; set; }
        public required string SecretKey { get; set; }
        public required bool WithSSL { get; set; }
    }

    public static class ObjectStorageServiceExtensions
    {
        public static void AddMinIOConfiguration(this IServiceCollection services, IConfiguration configuration)
        {
            services.Configure<MinIOVariables>(configuration.GetSection("MinIO"));
        }

        public static void ConfigureMinIOClient(this IServiceCollection services, MinIOVariables minIOVariables)
        {
            services.AddMinio(configureClient => configureClient
           .WithEndpoint(minIOVariables.Endpoint)
           .WithCredentials(minIOVariables.AccessKey, minIOVariables.SecretKey)
           .WithSSL(minIOVariables.WithSSL)
           .Build());
        }

        public static void AddIObjectStorageService(this IServiceCollection services)
        {
            services.AddSingleton<IObjectStorageService, MinIOService>();
        }
    }
}