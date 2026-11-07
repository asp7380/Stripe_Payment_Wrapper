using System.ComponentModel.DataAnnotations;

namespace StripeApiWrapper.Api.Options;

public sealed class StripeOptions
{
    [Required]
    public string SecretKey { get; init; } = string.Empty;

    // Signing secret (whsec_...) for verifying Stripe webhook signatures.
    // Optional so local/dev and tests can run without webhooks; the webhook
    // endpoint rejects calls when this is unset.
    public string? WebhookSecret { get; init; }

    // Where the webhook forwards fulfillment (e.g. the frontend's /api/orders).
    // Kept generic so this service has no Strapi dependency. When unset, the
    // webhook verifies and logs but performs no fulfillment.
    public string? FulfillmentCallbackUrl { get; init; }

    // Optional shared secret sent as X-Fulfillment-Secret so the callback can
    // reject arbitrary callers.
    public string? FulfillmentCallbackSecret { get; init; }
}
