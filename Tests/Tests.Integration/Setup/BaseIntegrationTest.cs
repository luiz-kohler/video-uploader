using Bogus;
using Microsoft.Extensions.DependencyInjection;

namespace Tests.Integration.Setup
{
    public class BaseIntegrationTest : IClassFixture<IntegrationTestWebAppFactory>
    {
        private readonly IServiceScope _scope;
        protected readonly Faker _faker;
        protected readonly HttpClient _httpClient;

        protected BaseIntegrationTest(IntegrationTestWebAppFactory factory)
        {
            _faker = new Faker();

            _httpClient = factory.HttpClient;
            _scope = factory.Services.CreateScope();
        }
    }
}
