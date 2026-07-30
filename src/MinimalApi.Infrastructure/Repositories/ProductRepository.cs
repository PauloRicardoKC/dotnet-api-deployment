using Dapper;
using MinimalApi.Application.Interfaces;
using MinimalApi.Domain.Entities;
using MinimalApi.Infrastructure.Persistence;
using MinimalApi.Infrastructure.Queries;

namespace MinimalApi.Infrastructure.Repositories;

public sealed class ProductRepository(IDbConnectionFactory connectionFactory) : IProductRepository
{
    public async Task<IReadOnlyCollection<Product>> GetAllAsync(CancellationToken cancellationToken)
    {
        using var connection = connectionFactory.CreateConnection();
        var products = await connection.QueryAsync<Product>(new CommandDefinition(ProductQueries.GetAll, cancellationToken: cancellationToken));
        return products.ToArray();
    }

    public async Task<Product?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        using var connection = connectionFactory.CreateConnection();
        return await connection.QuerySingleOrDefaultAsync<Product>(new CommandDefinition(ProductQueries.GetById, new { Id = id }, cancellationToken: cancellationToken));
    }

    public async Task AddAsync(Product product, CancellationToken cancellationToken)
    {
        using var connection = connectionFactory.CreateConnection();
        await connection.ExecuteAsync(new CommandDefinition(ProductQueries.Add, product, cancellationToken: cancellationToken));
    }

    public async Task UpdateAsync(Product product, CancellationToken cancellationToken)
    {
        using var connection = connectionFactory.CreateConnection();
        await connection.ExecuteAsync(new CommandDefinition(ProductQueries.Update, product, cancellationToken: cancellationToken));
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        using var connection = connectionFactory.CreateConnection();
        return await connection.ExecuteAsync(new CommandDefinition(ProductQueries.Delete, new { Id = id }, cancellationToken: cancellationToken)) > 0;
    }
}
