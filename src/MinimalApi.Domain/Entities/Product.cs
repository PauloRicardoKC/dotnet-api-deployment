namespace MinimalApi.Domain.Entities;

public sealed class Product
{
    public Guid Id { get; init; }
    public string Name { get; private set; }
    public string? Description { get; private set; }
    public decimal Price { get; private set; }
    public int Stock { get; private set; }
    public DateTime CreatedAt { get; init; }

    public Product(Guid id, string name, string? description, decimal price, int stock, DateTime createdAt)
    {
        Id = id;
        Name = name;
        Description = description;
        Price = price;
        Stock = stock;
        CreatedAt = createdAt;
    }

    public void Update(string name, string? description, decimal price, int stock)
    {
        Name = name;
        Description = description;
        Price = price;
        Stock = stock;
    }
}
