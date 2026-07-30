using MinimalApi.Application.DTOs;

namespace MinimalApi.Application.Interfaces;

public interface IProductService
{
    Task<IReadOnlyCollection<ProductResponse>> GetAllAsync(CancellationToken cancellationToken);
    Task<ProductResponse?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<ProductResponse> CreateAsync(ProductRequest request, CancellationToken cancellationToken);
    Task<bool> UpdateAsync(Guid id, ProductRequest request, CancellationToken cancellationToken);
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken);
}
