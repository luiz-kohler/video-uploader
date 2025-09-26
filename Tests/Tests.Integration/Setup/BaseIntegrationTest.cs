using Bogus;
using Microsoft.Extensions.DependencyInjection;
using Minio;

namespace Tests.Integration.Setup
{
    public class BaseIntegrationTest : IClassFixture<IntegrationTestWebAppFactory>
    {
        private readonly IServiceScope _scope;
        protected readonly Faker _faker;
        protected readonly IMinioClient _minioClient;
        protected readonly HttpClient _httpClient;

        protected BaseIntegrationTest(IntegrationTestWebAppFactory factory)
        {
            _faker = new Faker();

            _httpClient = factory.HttpClient;
            _scope = factory.Services.CreateScope();

            var minioFactory = _scope.ServiceProvider.GetService<IMinioClientFactory>();
            if(minioFactory == null)
                throw new ArgumentNullException(nameof(minioFactory));

            _minioClient = minioFactory.CreateClient();
        }
    }
}
