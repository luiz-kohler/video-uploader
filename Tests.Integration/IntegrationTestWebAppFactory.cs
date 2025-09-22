using DotNet.Testcontainers.Builders;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.VisualStudio.TestPlatform.TestHost;

namespace Tests.Integration
{
    public class IntegrationTestWebAppFactory : WebApplicationFactory<Program>
    {
        private readonly ContainerBuilder _minIOContainer = new ContainerBuilder();

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
        }
    }
}
