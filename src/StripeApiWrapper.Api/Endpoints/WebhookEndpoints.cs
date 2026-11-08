using Stripe;
using StripeApiWrapper.Api.Services;

namespace StripeApiWrapper.Api.Endpoints;

public static class WebhookEndpoints
{
    public static void MapWebhookEndpoints(this WebApplication app)
    {
        app.MapPost("/api/webhooks/stripe", async (
            HttpRequest request,
            IStripeEventReader eventReader,
            IFulfillmentNotifier fulfillmentNotifier,
            ILogger<Program> logger,
            CancellationToken cancellationToken) =>
        {
            // The raw body is required for signature verification, so read it
            // directly rather than binding a model.
            using var reader = new StreamReader(request.Body);
            var json = await reader.ReadToEndAsync(cancellationToken);
            var signature = request.Headers["Stripe-Signature"].ToString();

            Event stripeEvent;
            try
            {
                stripeEvent = eventReader.Construct(json, signature);
            }
            catch (StripeException ex)
            {
                logger.LogWarning(ex, "Rejected Stripe webhook: signature verification failed.");
                return Results.BadRequest();
            }

            if (stripeEvent.Type == "payment_intent.succeeded"
                && stripeEvent.Data.Object is PaymentIntent paymentIntent)
            {
                try
                {
                    await fulfillmentNotifier.NotifyAsync(paymentIntent, cancellationToken);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex,
                        "Fulfillment failed for payment {PaymentIntentId}; returning 500 so Stripe retries.",
                        paymentIntent.Id);
                    return Results.StatusCode(StatusCodes.Status500InternalServerError);
                }
            }
            else
            {
                logger.LogInformation("Ignoring unhandled Stripe event type {EventType}.", stripeEvent.Type);
            }

            return Results.Ok();
        });
    }
}
