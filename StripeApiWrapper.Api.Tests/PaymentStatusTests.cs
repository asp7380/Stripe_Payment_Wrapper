using System.Text.Json;
using StripeApiWrapper.Api.Contracts;

namespace StripeApiWrapper.Api.Tests;

public class PaymentStatusTests
{
    // The client depends on these exact snake_case wire values (matching Stripe).
    [Theory]
    [InlineData(PaymentStatus.Succeeded, "succeeded")]
    [InlineData(PaymentStatus.RequiresAction, "requires_action")]
    [InlineData(PaymentStatus.RequiresPaymentMethod, "requires_payment_method")]
    [InlineData(PaymentStatus.Processing, "processing")]
    [InlineData(PaymentStatus.Canceled, "canceled")]
    public void Serializes_ToSnakeCaseWireValue(PaymentStatus status, string wire)
    {
        Assert.Equal($"\"{wire}\"", JsonSerializer.Serialize(status));
    }

    [Theory]
    [InlineData("succeeded", PaymentStatus.Succeeded)]
    [InlineData("requires_action", PaymentStatus.RequiresAction)]
    [InlineData("requires_payment_method", PaymentStatus.RequiresPaymentMethod)]
    [InlineData("something_unexpected", PaymentStatus.Unknown)]
    [InlineData(null, PaymentStatus.Unknown)]
    public void FromStripe_MapsWireValue_OrUnknown(string? stripeStatus, PaymentStatus expected)
    {
        Assert.Equal(expected, PaymentStatusMapper.FromStripe(stripeStatus));
    }
}
