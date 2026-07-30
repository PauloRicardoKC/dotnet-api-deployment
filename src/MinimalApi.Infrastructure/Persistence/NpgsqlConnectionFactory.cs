using System.Data;
using Microsoft.Extensions.Options;
using Npgsql;

namespace MinimalApi.Infrastructure.Persistence;

public sealed class NpgsqlConnectionFactory(IOptions<DatabaseOptions> options) : IDbConnectionFactory
{
    public IDbConnection CreateConnection() => new NpgsqlConnection(options.Value.ConnectionString);
}
