using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Testcontainers.Minio;

namespace Tests.Integration.Setup
{
    public class IntegrationTestWebAppFactory : WebApplicationFactory<Program>, IAsyncLifetime
    {
        public HttpClient HttpClient { get; private set; } = null!;
        protected IContainer _minioContainer { get; set; } = BuildMinIOContainer();

        protected override void ConfigureWebHost(IWebHostBuilder builder) 
        {
            var x = _minioContainer.GetMappedPublicPorts();

            Environment.SetEnvironmentVariable("MinIO:Endpoint", $"localhost:{_minioContainer.GetMappedPublicPort()}");
            Environment.SetEnvironmentVariable("MinIO:AccessKey", $"minioadmin");
            Environment.SetEnvironmentVariable("MinIO:SecretKey", $"minioadmin");
            Environment.SetEnvironmentVariable("MinIO:BucketName", $"video-uploader-tests");
        }

        public async Task InitializeAsync()
        {
            await _minioContainer.StartAsync();
            HttpClient = CreateClient();
        }

        public new async Task DisposeAsync()
        {
            HttpClient?.Dispose();
            await _minioContainer.StopAsync();
            await _minioContainer.DisposeAsync();
        }

        private static IContainer BuildMinIOContainer()
        {
            return new MinioBuilder()
                .WithImage("quay.io/minio/minio:latest")
                .WithCommand("--console-address", ":9001")
                .WithPortBinding(9000, true) // API port
                .WithPortBinding(9001, true) // Console port
                .WithEnvironment("MINIO_ROOT_USER", "minioadmin")
                .WithEnvironment("MINIO_ROOT_PASSWORD", "minioadmin")
                .WithWaitStrategy(Wait.ForUnixContainer().UntilMessageIsLogged(".*API.*"))
                .Build();
        }
    }
}
