using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using FluentValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using MinimalApi.Api.Endpoints;
using MinimalApi.Application.DTOs;
using MinimalApi.Application.Interfaces;
using MinimalApi.Application.Validators;

namespace MinimalApi.UnitTests;

public sealed class ProductEndpointsTests
{
    [Fact]
    public async Task GetEndpoints_ShouldReturnOkForExistingDataAndNotFoundForMissingProduct()
    {
        var product = CreateResponse();
        var service = new FakeProductService { Products = [product], ProductToReturn = product };
        await using var app = await CreateAppAsync(service);
        var client = app.GetTestClient();

        var listResponse = await client.GetAsync("/products/");
        var productResponse = await client.GetAsync($"/products/{product.Id}");
        service.ProductToReturn = null;
        var missingResponse = await client.GetAsync($"/products/{Guid.NewGuid()}");

        listResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        (await listResponse.Content.ReadFromJsonAsync<List<ProductResponse>>()).Should().ContainSingle().Which.Id.Should().Be(product.Id);
        productResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        missingResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task PostEndpoint_ShouldCreateValidProductAndRejectInvalidRequest()
    {
        var product = CreateResponse();
        var service = new FakeProductService { CreatedProduct = product };
        await using var app = await CreateAppAsync(service);
        var client = app.GetTestClient();

        var createdResponse = await client.PostAsJsonAsync("/products/", new ProductRequest("Keyboard", "Mechanical", 299.90m, 10));
        var invalidResponse = await client.PostAsJsonAsync("/products/", new ProductRequest("", null, 10, 1));

        createdResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        createdResponse.Headers.Location!.OriginalString.Should().Be($"/products/{product.Id}");
        invalidResponse.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        service.CreateCalls.Should().Be(1);
    }

    [Fact]
    public async Task PutEndpoint_ShouldReturnNoContentOrNotFoundAndRejectInvalidRequest()
    {
        var service = new FakeProductService { UpdateResult = true };
        await using var app = await CreateAppAsync(service);
        var client = app.GetTestClient();
        var request = new ProductRequest("Keyboard", null, 10, 1);

        var updatedResponse = await client.PutAsJsonAsync($"/products/{Guid.NewGuid()}", request);
        service.UpdateResult = false;
        var missingResponse = await client.PutAsJsonAsync($"/products/{Guid.NewGuid()}", request);
        var invalidResponse = await client.PutAsJsonAsync($"/products/{Guid.NewGuid()}", new ProductRequest("", null, 10, 1));

        updatedResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);
        missingResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
        invalidResponse.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        service.UpdateCalls.Should().Be(2);
    }

    [Fact]
    public async Task DeleteEndpoint_ShouldReturnNoContentOrNotFound()
    {
        var service = new FakeProductService { DeleteResult = true };
        await using var app = await CreateAppAsync(service);
        var client = app.GetTestClient();

        var deletedResponse = await client.DeleteAsync($"/products/{Guid.NewGuid()}");
        service.DeleteResult = false;
        var missingResponse = await client.DeleteAsync($"/products/{Guid.NewGuid()}");

        deletedResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);
        missingResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
        service.DeleteCalls.Should().Be(2);
    }

    private static async Task<WebApplication> CreateAppAsync(FakeProductService service)
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Services.AddSingleton<IProductService>(service);
        builder.Services.AddSingleton<IValidator<ProductRequest>, ProductValidator>();
        var app = builder.Build();
        app.MapProductEndpoints();
        await app.StartAsync();
        return app;
    }

    private static ProductResponse CreateResponse() => new(Guid.NewGuid(), "Keyboard", "Mechanical", 299.90m, 10, DateTime.UtcNow);

    private sealed class FakeProductService : IProductService
    {
        public IReadOnlyCollection<ProductResponse> Products { get; init; } = [];
        public ProductResponse? ProductToReturn { get; set; }
        public ProductResponse CreatedProduct { get; init; } = CreateResponse();
        public bool UpdateResult { get; set; }
        public bool DeleteResult { get; set; }
        public int CreateCalls { get; private set; }
        public int UpdateCalls { get; private set; }
        public int DeleteCalls { get; private set; }

        public Task<IReadOnlyCollection<ProductResponse>> GetAllAsync(CancellationToken cancellationToken) => Task.FromResult(Products);
        public Task<ProductResponse?> GetByIdAsync(Guid id, CancellationToken cancellationToken) => Task.FromResult(ProductToReturn);
        public Task<ProductResponse> CreateAsync(ProductRequest request, CancellationToken cancellationToken)
        {
            CreateCalls++;
            return Task.FromResult(CreatedProduct);
        }

        public Task<bool> UpdateAsync(Guid id, ProductRequest request, CancellationToken cancellationToken)
        {
            UpdateCalls++;
            return Task.FromResult(UpdateResult);
        }

        public Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken)
        {
            DeleteCalls++;
            return Task.FromResult(DeleteResult);
        }
    }
}
