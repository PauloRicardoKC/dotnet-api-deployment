using FluentValidation;
using MinimalApi.Application.DTOs;

namespace MinimalApi.Application.Validators;

public sealed class ProductValidator : AbstractValidator<ProductRequest>
{
    public ProductValidator()
    {
        RuleFor(product => product.Name).NotEmpty().MaximumLength(150);
        RuleFor(product => product.Description).MaximumLength(1000);
        RuleFor(product => product.Price).GreaterThanOrEqualTo(0);
        RuleFor(product => product.Stock).GreaterThanOrEqualTo(0);
    }
}