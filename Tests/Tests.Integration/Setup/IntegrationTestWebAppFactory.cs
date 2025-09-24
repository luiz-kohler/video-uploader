using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Minio;
using Testcontainers.Minio;

namespace Tests.Integration.Setup
{
    public class IntegrationTestWebAppFactory : WebApplicationFactory<Program>, IAsyncLifetime
    {
        public HttpClient HttpClient { get; private set; } = null!;
        protected IContainer _minioContainer { get; set; } = MinioTestConfiguration.BuildConfiguredMinIOContainer();

        protected override void ConfigureWebHost(IWebHostBuilder builder) 
        {
            MinioTestConfiguration.UpdateEnvVariables(_minioContainer);
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
    }
}
