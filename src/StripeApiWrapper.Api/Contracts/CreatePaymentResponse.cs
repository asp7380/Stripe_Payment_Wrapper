namespace StripeApiWrapper.Api.Contracts;

public sealed record CreatePaymentResponse(
    string PaymentIntentId,
    PaymentStatus Status,
    string? ClientSecret);
