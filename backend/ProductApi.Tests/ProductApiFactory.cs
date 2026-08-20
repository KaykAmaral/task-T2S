using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using ProductApi.Services;

namespace ProductApi.Tests;

public sealed class ProductApiFactory : WebApplicationFactory<Program>
{
    public FakeProductService ProductService { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureLogging(logging => logging.ClearProviders());

        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<IProductService>();
            services.AddSingleton(ProductService);
            services.AddSingleton<IProductService>(ProductService);
        });
    }
}
