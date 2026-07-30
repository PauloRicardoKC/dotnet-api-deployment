using FluentAssertions;
using MinimalApi.Application.DTOs;
using MinimalApi.Application.Interfaces;
using MinimalApi.Application.Services;
using MinimalApi.Domain.Entities;

namespace MinimalApi.UnitTests;

public sealed class ProductServiceTests
{
    [Fact]
    public async Task GetAllAsync_ShouldMapRepositoryProductsToResponses()
    {
        var product = CreateProduct();
        var repository = new FakeProductRepository { Products = [product] };

        var result = await new ProductService(repository).GetAllAsync(CancellationToken.None);

        result.Should().BeEquivalentTo([ToResponse(product)]);
    }

    [Fact]
    public async Task GetByIdAsync_WhenProductExists_ShouldReturnMappedResponse()
    {
        var product = CreateProduct();
        var repository = new FakeProductRepository { ProductToReturn = product };

        var result = await new ProductService(repository).GetByIdAsync(product.Id, CancellationToken.None);

        result.Should().BeEquivalentTo(ToResponse(product));
        repository.LastRequestedId.Should().Be(product.Id);
    }

    [Fact]
    public async Task GetByIdAsync_WhenProductDoesNotExist_ShouldReturnNull()
    {
        var result = await new ProductService(new FakeProductRepository()).GetByIdAsync(Guid.NewGuid(), CancellationToken.None);

        result.Should().BeNull();
    }

    [Fact]
    public async Task CreateAsync_ShouldPersistAndReturnTheNewProduct()
    {
        var repository = new FakeProductRepository();
        var request = new ProductRequest("Keyboard", "Mechanical", 299.90m, 10);

        var result = await new ProductService(repository).CreateAsync(request, CancellationToken.None);

        repository.AddedProduct.Should().NotBeNull();
        result.Id.Should().Be(repository.AddedProduct!.Id);
        result.Name.Should().Be(request.Name);
        result.Description.Should().Be(request.Description);
        result.Price.Should().Be(request.Price);
        result.Stock.Should().Be(request.Stock);
        result.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(2));
    }

    [Fact]
    public async Task UpdateAsync_WhenProductExists_ShouldUpdateAndPersistIt()
    {
        var product = CreateProduct();
        var repository = new FakeProductRepository { ProductToReturn = product };
        var request = new ProductRequest("Mouse", "Wireless", 149.90m, 4);

        var result = await new ProductService(repository).UpdateAsync(product.Id, request, CancellationToken.None);

        result.Should().BeTrue();
        repository.UpdatedProduct.Should().BeSameAs(product);
        product.Name.Should().Be(request.Name);
        product.Description.Should().Be(request.Description);
        product.Price.Should().Be(request.Price);
        product.Stock.Should().Be(request.Stock);
    }

    [Fact]
    public async Task UpdateAsync_WhenProductDoesNotExist_ShouldNotPersist()
    {
        var repository = new FakeProductRepository();

        var result = await new ProductService(repository).UpdateAsync(Guid.NewGuid(), new ProductRequest("Mouse", null, 10, 1), CancellationToken.None);

        result.Should().BeFalse();
        repository.UpdatedProduct.Should().BeNull();
    }

    [Fact]
    public async Task DeleteAsync_ShouldReturnRepositoryResult()
    {
        var id = Guid.NewGuid();
        var repository = new FakeProductRepository { DeleteResult = true };

        var result = await new ProductService(repository).DeleteAsync(id, CancellationToken.None);

        result.Should().BeTrue();
        repository.LastDeletedId.Should().Be(id);
    }

    private static Product CreateProduct() => new(Guid.NewGuid(), "Keyboard", "Mechanical", 299.90m, 10, DateTime.UtcNow.AddDays(-1));
    private static ProductResponse ToResponse(Product product) => new(product.Id, product.Name, product.Description, product.Price, product.Stock, product.CreatedAt);

    private sealed class FakeProductRepository : IProductRepository
    {
        public IReadOnlyCollection<Product> Products { get; init; } = [];
        public Product? ProductToReturn { get; init; }
        public Product? AddedProduct { get; private set; }
        public Product? UpdatedProduct { get; private set; }
        public Guid? LastRequestedId { get; private set; }
        public Guid? LastDeletedId { get; private set; }
        public bool DeleteResult { get; init; }

        public Task<IReadOnlyCollection<Product>> GetAllAsync(CancellationToken cancellationToken) => Task.FromResult(Products);
        public Task<Product?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
        {
            LastRequestedId = id;
            return Task.FromResult(ProductToReturn);
        }

        public Task AddAsync(Product product, CancellationToken cancellationToken)
        {
            AddedProduct = product;
            return Task.CompletedTask;
        }

        public Task UpdateAsync(Product product, CancellationToken cancellationToken)
        {
            UpdatedProduct = product;
            return Task.CompletedTask;
        }

        public Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken)
        {
            LastDeletedId = id;
            return Task.FromResult(DeleteResult);
        }
    }
}
