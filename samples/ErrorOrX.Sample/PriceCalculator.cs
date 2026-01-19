namespace ErrorOrX.Sample;

public class PriceCalculator
{
    public decimal CalculatePrice(decimal price, decimal discountPercent)
    {
        if (price <= 0)
        {
            throw new ArgumentException("Price must be greater than zero.");
        }

        if (discountPercent < 0 || discountPercent > 100)
        {
            throw new ArgumentException("Discount percent must be between 0 and 100.");
        }

        var discount = price * (discountPercent / 100);
        var discountedPrice = price - discount;

        return Math.Round(discountedPrice, 2);
    }
}
