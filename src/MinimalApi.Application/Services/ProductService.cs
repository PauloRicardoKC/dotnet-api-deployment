using MinimalApi.Application.DTOs;
using MinimalApi.Application.Interfaces;
using MinimalApi.Domain.Entities;

namespace MinimalApi.Application.Services;

public sealed class ProductService(IProductRepository repository) : IProductService
{
    public async Task<IReadOnlyCollection<ProductResponse>> GetAllAsync(CancellationToken cancellationToken) =>
        (await repository.GetAllAsync(cancellationToken)).Select(ToResponse).ToArray();

    public async Task<ProductResponse?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var product = await repository.GetByIdAsync(id, cancellationToken);
        return product is null ? null : ToResponse(product);
    }

    public async Task<ProductResponse> CreateAsync(ProductRequest request, CancellationToken cancellationToken)
    {
        var product = new Product(Guid.NewGuid(), request.Name, request.Description, request.Price, request.Stock, DateTime.UtcNow);
        await repository.AddAsync(product, cancellationToken);
        return ToResponse(product);
    }

    public async Task<bool> UpdateAsync(Guid id, ProductRequest request, CancellationToken cancellationToken)
    {
        var product = await repository.GetByIdAsync(id, cancellationToken);
        if (product is null) return false;
        product.Update(request.Name, request.Description, request.Price, request.Stock);
        await repository.UpdateAsync(product, cancellationToken);
        return true;
    }

    public Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken) => repository.DeleteAsync(id, cancellationToken);

    private static ProductResponse ToResponse(Product product) =>
        new(product.Id, product.Name, product.Description, product.Price, product.Stock, product.CreatedAt);
}
