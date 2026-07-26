namespace StripeApiWrapper.Api.Contracts;

public sealed record CreatePaymentRequest(
    decimal Amount,
    string Currency,
    string PaymentMethodId,
    string? Description);
