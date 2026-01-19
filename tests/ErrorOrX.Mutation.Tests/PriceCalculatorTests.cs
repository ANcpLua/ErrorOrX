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
    public void InvalidPrice_ShouldThrowException()
    {
        var calculator = new PriceCalculator();

        Assert.Throws<ArgumentException>(() => calculator.CalculatePrice(-10, 10));
    }
}
