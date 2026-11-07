using Microsoft.Extensions.Options;
using Stripe;
using StripeApiWrapper.Api.Options;

namespace StripeApiWrapper.Api.Services;

public sealed class StripeEventReader(IOptions<StripeOptions> options) : IStripeEventReader
{
    private readonly StripeOptions _options = options.Value;

    public Event Construct(string json, string signatureHeader)
    {
        if (string.IsNullOrEmpty(_options.WebhookSecret))
        {
            throw new StripeException("Stripe webhook secret is not configured.");
        }

        return EventUtility.ConstructEvent(
            json,
            signatureHeader,
            _options.WebhookSecret,
            throwOnApiVersionMismatch: false);
    }
}
