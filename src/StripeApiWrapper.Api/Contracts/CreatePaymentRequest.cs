namespace StripeApiWrapper.Api.Contracts;

public sealed record CreatePaymentRequest(
    decimal Amount,
    string Currency,
    string PaymentMethodId,
    string? Description,
    // Attached to the PaymentIntent so the webhook can identify which order
    // succeeded (e.g. productSlug, quantity). Never put cardholder data here.
    IReadOnlyDictionary<string, string>? Metadata = null,
    // Optional customer + shipping details. When present, the payment is tied to
    // a Stripe Customer and a local record is upserted on success.
    CustomerInfo? Customer = null);
