using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.VisualStudio.TestPlatform.TestHost;

namespace Tests.Integration
{
    public class IntegrationTestWebAppFactory : WebApplicationFactory<Program>
    {
        private readonly IContainer _minIOContainer;

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
        }
    }
}
