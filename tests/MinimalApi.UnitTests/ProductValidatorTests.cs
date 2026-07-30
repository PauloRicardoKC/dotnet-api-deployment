using FluentAssertions;
using MinimalApi.Application.DTOs;
using MinimalApi.Application.Validators;

namespace MinimalApi.UnitTests;

public sealed class ProductValidatorTests
{
    [Fact]
    public void Validate_WhenProductIsValid_ShouldSucceed()
    {
        var result = new ProductValidator().Validate(new ProductRequest("Keyboard", "Mechanical", 299.90m, 10));
        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("", "Description", 10, 1, "Name")]
    [InlineData("Keyboard", "Description", -0.01, 1, "Price")]
    [InlineData("Keyboard", "Description", 10, -1, "Stock")]
    public void Validate_WhenProductHasInvalidValues_ShouldFail(string name, string? description, decimal price, int stock, string propertyName)
    {
        var result = new ProductValidator().Validate(new ProductRequest(name, description, price, stock));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(error => error.PropertyName == propertyName);
    }

    [Fact]
    public void Validate_WhenNameOrDescriptionExceedsMaximumLength_ShouldFail()
    {
        var request = new ProductRequest(new string('n', 151), new string('d', 1001), 10, 1);

        var result = new ProductValidator().Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(error => error.PropertyName == "Name");
        result.Errors.Should().Contain(error => error.PropertyName == "Description");
    }
}
