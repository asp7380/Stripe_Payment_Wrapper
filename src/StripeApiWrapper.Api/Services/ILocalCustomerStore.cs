using Stripe;

namespace StripeApiWrapper.Api.Services;

public interface ILocalCustomerStore
{
    // Upserts the local Customer/Address rows from a succeeded PaymentIntent's
    // metadata (written only on success). No-op if the PaymentIntent carries no
    // customer metadata.
    Task UpsertFromPaymentAsync(PaymentIntent paymentIntent, CancellationToken cancellationToken);
}
