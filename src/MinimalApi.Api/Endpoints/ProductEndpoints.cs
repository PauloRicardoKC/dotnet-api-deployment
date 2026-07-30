using FluentValidation;
using MinimalApi.Application.DTOs;
using MinimalApi.Application.Interfaces;

namespace MinimalApi.Api.Endpoints;

public static class ProductEndpoints
{
    public static IEndpointRouteBuilder MapProductEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/products").WithTags("Products");

        group.MapGet("/", async (IProductService service, CancellationToken cancellationToken) =>
            Results.Ok(await service.GetAllAsync(cancellationToken)));

        group.MapGet("/{id:guid}", async (Guid id, IProductService service, CancellationToken cancellationToken) =>
        {
            var product = await service.GetByIdAsync(id, cancellationToken);
            return product is null ? Results.NotFound() : Results.Ok(product);
        });

        group.MapPost("/", async (ProductRequest request, IValidator<ProductRequest> validator, IProductService service, CancellationToken cancellationToken) =>
        {
            var validation = await validator.ValidateAsync(request, cancellationToken);
            if (!validation.IsValid) return Results.ValidationProblem(validation.ToDictionary());
            var product = await service.CreateAsync(request, cancellationToken);
            return Results.Created($"/products/{product.Id}", product);
        });

        group.MapPut("/{id:guid}", async (Guid id, ProductRequest request, IValidator<ProductRequest> validator, IProductService service, CancellationToken cancellationToken) =>
        {
            var validation = await validator.ValidateAsync(request, cancellationToken);
            if (!validation.IsValid) return Results.ValidationProblem(validation.ToDictionary());
            return await service.UpdateAsync(id, request, cancellationToken) ? Results.NoContent() : Results.NotFound();
        });

        group.MapDelete("/{id:guid}", async (Guid id, IProductService service, CancellationToken cancellationToken) =>
            await service.DeleteAsync(id, cancellationToken) ? Results.NoContent() : Results.NotFound());

        return endpoints;
    }
}
