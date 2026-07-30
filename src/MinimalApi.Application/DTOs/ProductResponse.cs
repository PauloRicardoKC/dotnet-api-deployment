namespace MinimalApi.Application.DTOs;

public sealed record ProductResponse(Guid Id, string Name, string? Description, decimal Price, int Stock, DateTime CreatedAt);
