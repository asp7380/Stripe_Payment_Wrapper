using Stripe;

namespace StripeApiWrapper.Api.Services;

// Seam over EventUtility.ConstructEvent so the webhook endpoint can be tested
// without computing real Stripe signatures.
public interface IStripeEventReader
{
    // Verifies the signature and deserializes the event. Throws StripeException
    // on a missing/invalid signature or an unconfigured secret.
    Event Construct(string json, string signatureHeader);
}
