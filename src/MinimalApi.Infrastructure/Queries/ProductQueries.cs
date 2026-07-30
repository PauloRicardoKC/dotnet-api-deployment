namespace MinimalApi.Infrastructure.Queries;

public static class ProductQueries
{
    public const string GetAll = """
        SELECT id AS Id, name AS Name, description AS Description, price AS Price, stock AS Stock, created_at AS CreatedAt
        FROM products
        ORDER BY created_at DESC;
        """;

    public const string GetById = """
        SELECT id AS Id, name AS Name, description AS Description, price AS Price, stock AS Stock, created_at AS CreatedAt
        FROM products
        WHERE id = @Id;
        """;

    public const string Add = """
        INSERT INTO products (id, name, description, price, stock, created_at)
        VALUES (@Id, @Name, @Description, @Price, @Stock, @CreatedAt);
        """;

    public const string Update = """
        UPDATE products
        SET name = @Name, description = @Description, price = @Price, stock = @Stock
        WHERE id = @Id;
        """;

    public const string Delete = """
        DELETE FROM products
        WHERE id = @Id;
        """;
}
