using System.Net.Http.Json;
using Microsoft.Extensions.Options;
using Stripe;
using StripeApiWrapper.Api.Options;

namespace StripeApiWrapper.Api.Services;

public sealed class HttpFulfillmentNotifier(
    HttpClient httpClient,
    IOptions<StripeOptions> options,
    ILogger<HttpFulfillmentNotifier> logger) : IFulfillmentNotifier
{
    private readonly StripeOptions _options = options.Value;

    public async Task NotifyAsync(PaymentIntent paymentIntent, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(_options.FulfillmentCallbackUrl))
        {
            logger.LogWarning(
                "Payment {PaymentIntentId} succeeded but no FulfillmentCallbackUrl is configured; skipping fulfillment.",
                paymentIntent.Id);
            return;
        }

        var metadata = paymentIntent.Metadata ?? new Dictionary<string, string>();
        if (!metadata.TryGetValue("productSlug", out var productSlug) || string.IsNullOrWhiteSpace(productSlug))
        {
            logger.LogWarning(
                "Payment {PaymentIntentId} succeeded but carries no productSlug metadata; nothing to fulfill.",
                paymentIntent.Id);
            return;
        }

        var quantity = 1;
        if (metadata.TryGetValue("quantity", out var quantityRaw)
            && int.TryParse(quantityRaw, out var parsed)
            && parsed > 0)
        {
            quantity = parsed;
        }

        using var message = new HttpRequestMessage(HttpMethod.Post, _options.FulfillmentCallbackUrl)
        {
            Content = JsonContent.Create(new
            {
                productSlug,
                quantity,
                paymentIntentId = paymentIntent.Id,
            }),
        };

        if (!string.IsNullOrEmpty(_options.FulfillmentCallbackSecret))
        {
            message.Headers.Add("X-Fulfillment-Secret", _options.FulfillmentCallbackSecret);
        }

        // Non-success throws, which the webhook endpoint turns into a 500 so
        // Stripe retries delivery.
        var response = await httpClient.SendAsync(message, cancellationToken);
        response.EnsureSuccessStatusCode();
    }
}
