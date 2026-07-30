using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using MinimalApi.Application.Interfaces;
using MinimalApi.Application.Services;
using MinimalApi.Application.Validators;

namespace MinimalApi.Application.DependencyInjection;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddValidatorsFromAssemblyContaining<ProductValidator>();
        services.AddScoped<IProductService, ProductService>();
        return services;
    }
}
