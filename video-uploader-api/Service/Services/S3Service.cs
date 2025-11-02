using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;
using Amazon.S3.Util;
using Castle.DynamicProxy;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using System.Net;
using System.Runtime.InteropServices;

namespace Service.Services
{
    public interface IS3Service
    {
        Task<string> StartMultiPart(string key, string fileName);
        string PreSignedPart(string key, string uploadId, int partNumber);
        Task CompleteMultiPart(string key, string uploadId, List<PartETagInfoDto> parts);
        Task EnsureBucketIsCreated();
    }

    public class S3Service : IS3Service
    {
        private readonly IAmazonS3 _client;
        private readonly S3Settings _settings;

        public S3Service(IOptions<S3Settings> options, IAmazonS3 amazonS3)
        {
            _settings = options.Value;
            _client = amazonS3;
        }

        public async Task<string> StartMultiPart(string key, string fileName)
        {
            var request = new InitiateMultipartUploadRequest
            {
                BucketName = _settings.BucketName,
                Key = key,
                ContentType = "video/mp4",
                Metadata =
                {
                    ["scan-status"] = "PENDING",
                    ["file-name"] = fileName
                }
            };

            var response = await _client.InitiateMultipartUploadAsync(request);

            return response.UploadId;
        }

        public string PreSignedPart(string key, string uploadId, int partNumber)
        {
            var request = new GetPreSignedUrlRequest
            {
                BucketName = _settings.BucketName,
                Key = key,
                Verb = HttpVerb.PUT,
                Expires = DateTime.UtcNow.AddMinutes(15),
                Protocol = Protocol.HTTP,
                UploadId = uploadId,
                PartNumber = partNumber
            };

            string preSignedUrl = _client.GetPreSignedURL(request);

            return preSignedUrl;
        }

        public async Task CompleteMultiPart(string key, string uploadId, List<PartETagInfoDto> parts)
        {
            var request = new CompleteMultipartUploadRequest
            {
                BucketName = _settings.BucketName,
                Key = key,
                UploadId = uploadId,
                PartETags = parts.Select(part => new PartETag(part.PartNumber, part.ETag)).ToList()
            };

            var response = await _client.CompleteMultipartUploadAsync(request);

            if (response.HttpStatusCode != HttpStatusCode.OK || string.IsNullOrEmpty(response.Location))
            {
                await AbortMultipartUpload(key, uploadId);
                throw new AmazonS3Exception("Multipart couldn't be completed. Try upload file again.", ErrorType.Sender, string.Empty, string.Empty, response.HttpStatusCode);
            }
        }

        private async Task AbortMultipartUpload(string key, string uploadId)
        {
            var abortRequest = new AbortMultipartUploadRequest
            {
                BucketName = _settings.BucketName,
                Key = key,
                UploadId = uploadId
            };

            await _client.AbortMultipartUploadAsync(abortRequest);
        }

        private async Task<bool> BucketExists()
        {
            return await AmazonS3Util.DoesS3BucketExistV2Async(_client, _settings.BucketName);
        }

        private async Task CreateBucket()
        {
            var request = new PutBucketRequest { BucketName = _settings.BucketName };
            var response = await _client.PutBucketAsync(request);

            if (response.HttpStatusCode != HttpStatusCode.OK)
                throw new AmazonS3Exception("Bucket couldn't be created.", ErrorType.Sender, string.Empty, string.Empty, response.HttpStatusCode);
        }

        public async Task EnsureBucketIsCreated()
        {
            var bucketExists = await BucketExists();
            if (!bucketExists)
                await CreateBucket();
        }
    }

    public class S3Settings
    {
        public required string BucketName { get; set; }
        public required string Endpoint { get; set; }
        public required string AccessKey { get; set; }
        public required string SecretKey { get; set; }
    }

    public static class S3ServiceExtensions
    {
        private static void AddS3Settings(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddOptions<S3Settings>().Bind(configuration.GetSection("MinIO"));
        }

        private static void ConfigureS3Client(this IServiceCollection services)
        {
            services.AddSingleton<IAmazonS3>(sp =>
            {
                var settings = sp.GetRequiredService<IOptions<S3Settings>>().Value;

                var config = new AmazonS3Config
                {
                    ServiceURL = $"http://{settings.Endpoint}",
                    ForcePathStyle = true
                };

                var baseClient = new AmazonS3Client(settings.AccessKey, settings.SecretKey, config);
                return S3ProxyFactory.Create(baseClient);
            });
        }

        private static void AddS3ServiceDI(this IServiceCollection services)
        {
            services.AddSingleton<IS3Service, S3Service>();
        }

        public static void AddS3Service(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddS3Settings(configuration);
            services.ConfigureS3Client();
            services.AddS3ServiceDI();
            services.AddHostedService<S3ServiceInitializer>();
        }
    }
    public class S3ServiceInitializer : IHostedService
    {
        private readonly IS3Service _s3Service;

        public S3ServiceInitializer(IS3Service s3Service)
        {
            _s3Service = s3Service;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            await _s3Service.EnsureBucketIsCreated();
        }

        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }

    public class S3ClientInterceptor : IInterceptor
    {
        public void Intercept(IInvocation invocation)
        {
            try { invocation.Proceed(); }
            catch { throw new ExternalException("MinIO not available right now."); }
        }
    }

    public static class S3ProxyFactory
    {
        private static readonly ProxyGenerator _generator = new ProxyGenerator();

        public static IAmazonS3 Create(IAmazonS3 innerClient)
        {
            var interceptor = new S3ClientInterceptor();
            return _generator.CreateInterfaceProxyWithTargetInterface(innerClient, interceptor);
        }
    }
}
