using Xunit;
using ErrorOrX.Sample;

namespace ErrorOrX.Mutation.Tests;

public class PriceCalculatorTests
{
    [Fact]
    public void ApplyDiscountCorrectly()
    {
        decimal price = 100;
        decimal discountPercent = 10;

        var calculator = new PriceCalculator();

        var result = calculator.CalculatePrice(price, discountPercent);

        Assert.Equal(90.00m, result);
    }

    [Fact]
    public void InvalidDiscountPercent_ShouldThrowException()
    {
        var calculator = new PriceCalculator();

        Assert.Throws<ArgumentException>(() => calculator.CalculatePrice(100, -1));
        Assert.Throws<ArgumentException>(() => calculator.CalculatePrice(100, 101));
    }

    [Fact]
    public void InvalidDiscountPercent_ShouldThrowExceptionWithCorrectMessage()
    {
        var calculator = new PriceCalculator();

        var ex1 = Assert.Throws<ArgumentException>(() => calculator.CalculatePrice(100, -1));
        Assert.Equal("Discount percent must be between 0 and 100.", ex1.Message);

        var ex2 = Assert.Throws<ArgumentException>(() => calculator.CalculatePrice(100, 101));
        Assert.Equal("Discount percent must be between 0 and 100.", ex2.Message);
    }

    [Fact]
    public void InvalidPrice_ShouldThrowException()
    {
        var calculator = new PriceCalculator();

        // Boundary check: 0 should fail
        Assert.Throws<ArgumentException>(() => calculator.CalculatePrice(0, 10));
    }

    [Fact]
    public void InvalidPrice_ShouldThrowExceptionWithCorrectMessage()
    {
        var calculator = new PriceCalculator();

        var ex = Assert.Throws<ArgumentException>(() => calculator.CalculatePrice(0, 10));
        Assert.Equal("Price must be greater than zero.", ex.Message);
    }

    [Fact]
    public void NoExceptionForZeroAnd100Discount()
    {
        var calculator = new PriceCalculator();

        var exceptionWhen0 = Record.Exception(() => calculator.CalculatePrice(100, 0));
        var exceptionWhen100 = Record.Exception(() => calculator.CalculatePrice(100, 100));

        Assert.Null(exceptionWhen0);
        Assert.Null(exceptionWhen100);
    }
}