using Stripe;

namespace StripeApiWrapper.Api.Services;

// Notifies the order/fulfillment system that a payment succeeded. Kept generic
// (no Strapi knowledge) so this service forwards to a configurable callback.
public interface IFulfillmentNotifier
{
    Task NotifyAsync(PaymentIntent paymentIntent, CancellationToken cancellationToken);
}
