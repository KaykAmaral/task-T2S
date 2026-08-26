using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using ProductApi.Controllers;
using ProductApi.DTOs;

namespace ProductApi.Tests;

public sealed class ProductsEndpointsTests : IClassFixture<ProductApiFactory>
{
    private readonly ProductApiFactory _factory;
    private readonly HttpClient _client;

    public ProductsEndpointsTests(ProductApiFactory factory)
    {
        _factory = factory;
        _factory.ProductService.Reset();
        _client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
    }

    [Fact]
    public async Task GetAll_ReturnsEmptyArray()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var response = await _client.GetAsync("/api/products", cancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var products = await response.Content
            .ReadFromJsonAsync<List<ProductResponse>>(cancellationToken);
        Assert.NotNull(products);
        Assert.Empty(products);
    }

    [Fact]
    public async Task Create_ReturnsCreatedProductAndLocation()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var request = new CreateProductRequest
        {
            Name = "Notebook",
            Price = 3500m
        };

        var response = await _client.PostAsJsonAsync(
            "/api/products",
            request,
            cancellationToken);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Equal("/api/products/1", response.Headers.Location?.AbsolutePath);

        var product = await response.Content
            .ReadFromJsonAsync<ProductResponse>(cancellationToken);
        Assert.Equal(new ProductResponse(1, "Notebook", 3500m), product);
    }

    [Fact]
    public async Task GetById_ReturnsProductOrNotFound()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        _factory.ProductService.Seed(new ProductResponse(7, "Keyboard", 250m));

        var foundResponse = await _client.GetAsync(
            "/api/products/7",
            cancellationToken);
        var missingResponse = await _client.GetAsync(
            "/api/products/999",
            cancellationToken);

        Assert.Equal(HttpStatusCode.OK, foundResponse.StatusCode);
        Assert.Equal(
            new ProductResponse(7, "Keyboard", 250m),
            await foundResponse.Content
                .ReadFromJsonAsync<ProductResponse>(cancellationToken));

        Assert.Equal(HttpStatusCode.NotFound, missingResponse.StatusCode);
        Assert.Equal(
            "application/problem+json",
            missingResponse.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task Update_ChangesExistingProductAndReturnsNotFoundForMissingProduct()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        _factory.ProductService.Seed(new ProductResponse(3, "Mouse", 100m));

        var request = new UpdateProductRequest
        {
            Name = "Gaming Mouse",
            Price = 180m
        };

        var updatedResponse = await _client.PutAsJsonAsync(
            "/api/products/3",
            request,
            cancellationToken);
        var productResponse = await _client.GetAsync(
            "/api/products/3",
            cancellationToken);
        var missingResponse = await _client.PutAsJsonAsync(
            "/api/products/999",
            request,
            cancellationToken);

        Assert.Equal(HttpStatusCode.NoContent, updatedResponse.StatusCode);
        Assert.Equal(
            new ProductResponse(3, "Gaming Mouse", 180m),
            await productResponse.Content
                .ReadFromJsonAsync<ProductResponse>(cancellationToken));
        Assert.Equal(HttpStatusCode.NotFound, missingResponse.StatusCode);
    }

    [Fact]
    public async Task Delete_RemovesExistingProductAndReturnsNotFoundAfterward()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        _factory.ProductService.Seed(new ProductResponse(5, "Monitor", 1200m));

        var deletedResponse = await _client.DeleteAsync(
            "/api/products/5",
            cancellationToken);
        var getResponse = await _client.GetAsync(
            "/api/products/5",
            cancellationToken);
        var missingDeleteResponse = await _client.DeleteAsync(
            "/api/products/5",
            cancellationToken);

        Assert.Equal(HttpStatusCode.NoContent, deletedResponse.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, missingDeleteResponse.StatusCode);
    }

    [Fact]
    public async Task Create_WithInvalidBody_ReturnsValidationProblem()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var request = new CreateProductRequest
        {
            Name = string.Empty,
            Price = 0m
        };

        var response = await _client.PostAsJsonAsync(
            "/api/products",
            request,
            cancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(
            "application/problem+json",
            response.Content.Headers.ContentType?.MediaType);

        var problem = await response.Content
            .ReadFromJsonAsync<ValidationProblemDetails>(cancellationToken);
        Assert.NotNull(problem);
        Assert.Contains("Name", problem.Errors.Keys);
        Assert.Contains("Price", problem.Errors.Keys);
    }

    [Theory]
    [InlineData("http://localhost:4200")]
    [InlineData("https://task-t2s.pages.dev")]
    public async Task Cors_AllowsConfiguredFrontendOrigin(string origin)
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        using var request = new HttpRequestMessage(HttpMethod.Options, "/api/products/1");
        request.Headers.Add("Origin", origin);
        request.Headers.Add("Access-Control-Request-Method", "PUT");
        request.Headers.Add("Access-Control-Request-Headers", "content-type");

        var response = await _client.SendAsync(request, cancellationToken);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Equal(
            origin,
            Assert.Single(response.Headers.GetValues("Access-Control-Allow-Origin")));
        Assert.Contains(
            "PUT",
            Assert.Single(response.Headers.GetValues("Access-Control-Allow-Methods")));
    }

    [Fact]
    public async Task UnexpectedException_ReturnsProblemDetailsWithoutInternalDetails()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        _factory.ProductService.Failure = new InvalidOperationException("sensitive detail");

        var response = await _client.GetAsync("/api/products", cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        Assert.Equal(
            "application/problem+json",
            response.Content.Headers.ContentType?.MediaType);
        Assert.DoesNotContain("sensitive detail", body);
        Assert.DoesNotContain("InvalidOperationException", body);
    }

    [Fact]
    public void Application_MapsExactlyTheFiveRequiredProductEndpoints()
    {
        _ = _client;

        var endpoints = _factory.Services
            .GetServices<EndpointDataSource>()
            .SelectMany(source => source.Endpoints)
            .OfType<RouteEndpoint>()
            .Where(endpoint =>
                endpoint.Metadata.GetMetadata<ControllerActionDescriptor>()?
                    .ControllerTypeInfo.AsType() == typeof(ProductsController))
            .SelectMany(endpoint =>
                endpoint.Metadata.GetMetadata<IHttpMethodMetadata>()!.HttpMethods
                    .Select(method => $"{method} {endpoint.RoutePattern.RawText}"))
            .Order()
            .ToArray();

        string[] expectedEndpoints =
        [
            "DELETE api/products/{id:long}",
            "GET api/products",
            "GET api/products/{id:long}",
            "POST api/products",
            "PUT api/products/{id:long}"
        ];

        Assert.Equal(expectedEndpoints, endpoints);
    }
}
