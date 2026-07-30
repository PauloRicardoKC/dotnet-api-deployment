namespace MinimalApi.Application.DTOs;

public sealed record ProductRequest(string Name, string? Description, decimal Price, int Stock);
