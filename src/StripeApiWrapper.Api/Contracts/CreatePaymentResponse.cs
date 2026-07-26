namespace StripeApiWrapper.Api.Contracts;

public sealed record CreatePaymentResponse(
    string PaymentIntentId,
    string Status,
    string? ClientSecret);
