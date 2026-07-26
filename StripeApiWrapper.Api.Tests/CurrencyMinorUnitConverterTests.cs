using StripeApiWrapper.Api.Services;

namespace StripeApiWrapper.Api.Tests;

public class CurrencyMinorUnitConverterTests
{
    [Theory]
    [InlineData(10.00, "usd", 1000)]
    [InlineData(10.00, "USD", 1000)]
    [InlineData(9.99, "eur", 999)]
    [InlineData(500, "jpy", 500)]
    [InlineData(10, "bhd", 10000)]
    [InlineData(5, "ugx", 500)]
    [InlineData(5, "isk", 500)]
    public void ToMinorUnits_ConvertsAccordingToCurrencyDecimalPlaces(decimal amount, string currency, long expected)
    {
        var actual = CurrencyMinorUnitConverter.ToMinorUnits(amount, currency);

        Assert.Equal(expected, actual);
    }

    [Theory]
    [InlineData(10.005, "usd")]
    [InlineData(500.5, "jpy")]
    [InlineData(10.0005, "bhd")]
    public void ToMinorUnits_Throws_WhenAmountHasMorePrecisionThanCurrencySupports(decimal amount, string currency)
    {
        Assert.Throws<ArgumentException>(() => CurrencyMinorUnitConverter.ToMinorUnits(amount, currency));
    }
}
