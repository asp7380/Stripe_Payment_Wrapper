namespace StripeApiWrapper.Api.Services;

public static class CurrencyMinorUnitConverter
{
    // Stripe charge amounts are always an integer in the currency's smallest unit.
    // ISK and UGX transitioned to zero-decimal currencies but still require the
    // amount multiplied by 100 for backward compatibility, so they are deliberately
    // NOT in this set. See https://docs.stripe.com/currencies#zero-decimal.
    private static readonly HashSet<string> ZeroDecimalCurrencies = new(StringComparer.OrdinalIgnoreCase)
    {
        "BIF", "CLP", "DJF", "GNF", "JPY", "KMF", "KRW",
        "MGA", "PYG", "RWF", "VND", "VUV", "XAF", "XOF", "XPF",
    };

    private static readonly HashSet<string> ThreeDecimalCurrencies = new(StringComparer.OrdinalIgnoreCase)
    {
        "BHD", "IQD", "JOD", "KWD", "LYD", "OMR", "TND",
    };

    public static long ToMinorUnits(decimal amount, string currency)
    {
        var factor = GetMinorUnitFactor(currency);
        var minorUnits = amount * factor;

        if (minorUnits != Math.Truncate(minorUnits))
        {
            throw new ArgumentException(
                $"Amount {amount} has more decimal precision than currency '{currency}' supports.",
                nameof(amount));
        }

        return (long)minorUnits;
    }

    private static int GetMinorUnitFactor(string currency)
    {
        if (ZeroDecimalCurrencies.Contains(currency))
        {
            return 1;
        }

        if (ThreeDecimalCurrencies.Contains(currency))
        {
            return 1000;
        }

        return 100;
    }
}
