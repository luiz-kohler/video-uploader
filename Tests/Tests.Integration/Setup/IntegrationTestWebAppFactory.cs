using DotNet.Testcontainers.Containers;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.VisualStudio.TestPlatform.TestHost;
using Testcontainers.Minio;

namespace Tests.Integration.Setup
{
    public class IntegrationTestWebAppFactory : WebApplicationFactory<Program>, IAsyncLifetime
    {
        public HttpClient HttpClient { get; private set; } = null!;

        private readonly IContainer _minioContainer = new MinioBuilder()
                                                         .WithImage("quay.io/minio/minio")
                                                         .Build();

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            Environment.SetEnvironmentVariable("MinIO:Endpoint", $"localhost:{_minioContainer.GetMappedPublicPort()}");
            Environment.SetEnvironmentVariable("MinIO:AccessKey", $"minioadmin");
            Environment.SetEnvironmentVariable("MinIO:SecretKey", $"minioadmin ");

            HttpClient = CreateClient();
        }
        public async Task InitializeAsync()
        {
            await _minioContainer.StartAsync();
        }

        public new async Task DisposeAsync()
        {
            await _minioContainer.DisposeAsync();
        }
    }
}
