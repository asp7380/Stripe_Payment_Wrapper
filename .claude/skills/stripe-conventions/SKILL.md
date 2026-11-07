---
name: stripe-conventions
description: Code conventions for the .NET Stripe payment microservice (stripe-payment-processor) — DI registration, PaymentIntent/webhook/fulfillment patterns, the metadata contract, currency handling, config, and PCI posture. Use when adding or changing payment endpoints, services, webhooks, or Stripe options in this repo.
---

# Stripe payment service conventions

ASP.NET Core **Minimal API**, **.NET 10**, `Stripe.net` **52.1.1**, `Scrutor` for DI scanning,
Swashbuckle (dev-only). No MVC controllers — endpoints are extension methods mapped in
`Program.cs`. All code under `src/StripeApiWrapper.Api/`.

## Layout & patterns
- **Endpoints**: `Endpoints/*Endpoints.cs`, each a static class with a
  `Map<Name>Endpoints(this WebApplication)` extension, called in `Program.cs`
  (`MapPaymentEndpoints`, `MapWebhookEndpoints`). Validate input, catch `StripeException`
  (→ `Results.Problem` 400) and `ArgumentException` (→ 400).
- **Services**: interface + `sealed` impl in `Services/`. Any class implementing
  `IPaymentService` is **auto-registered scoped by Scrutor** — just add the class, no manual
  DI. Services needing a typed `HttpClient` or other wiring (e.g. `IFulfillmentNotifier` →
  `HttpFulfillmentNotifier`) are registered explicitly (`AddHttpClient<,>`), as is
  `IStripeEventReader`.
- **Contracts**: `sealed record` request/response DTOs in `Contracts/`. Add new fields as
  **trailing optional params** to stay positional-compatible with existing callers/tests.
- **Options**: `Options/StripeOptions.cs`, bound from config section `Stripe`,
  `ValidateDataAnnotations().ValidateOnStart()`. `SecretKey` is `[Required]`; webhook/
  fulfillment settings are optional so dev/tests boot without them.
- **Program.cs** ends with `public partial class Program;` so tests can use
  `WebApplicationFactory<Program>`.

## Payment creation
`StripePaymentService` uses the **PaymentIntent** API with `Confirm = true` (create+confirm in
one call), `AutomaticPaymentMethods { Enabled = true, AllowRedirects = "never" }`, taking a
pre-created `PaymentMethodId` from the caller. Cards + Apple/Google Pay all arrive as
card-type PaymentMethods, so this one path serves all three.

## Metadata contract (for webhook fulfillment)
`CreatePaymentRequest.Metadata` (optional `IReadOnlyDictionary<string,string>`) is copied onto
the PaymentIntent. The storefront BFF sends `{ productSlug, quantity }`. The webhook reads
these back to fulfill. **Never put cardholder data in metadata.**

## Webhook + fulfillment
`WebhookEndpoints` (`POST /api/webhooks/stripe`): read the **raw body** (needed for signature
verification — don't model-bind), verify via `IStripeEventReader` (wraps
`EventUtility.ConstructEvent`; `throwOnApiVersionMismatch: false`). On
`"payment_intent.succeeded"` call `IFulfillmentNotifier`. Bad/missing signature → 400;
fulfillment exception → 500 (Stripe retries); unhandled event → log + 200.

`HttpFulfillmentNotifier` POSTs `{ productSlug, quantity, paymentIntentId }` to
`FulfillmentCallbackUrl` (optional `X-Fulfillment-Secret` header). It is deliberately
**Strapi-agnostic** — it knows only a generic callback URL, not Strapi. Keep it that way
(the project intends to decouple from Strapi). Missing URL or missing `productSlug` → log a
warning and no-op (don't make Stripe retry forever).

The event-type check uses the literal string `"payment_intent.succeeded"` — the `Events.*`
constant class is not present in this Stripe.net version.

## Currency
`CurrencyMinorUnitConverter.ToMinorUnits(decimal, currency)` converts major→minor units with
explicit zero-decimal (JPY, KRW, …) and three-decimal (BHD, KWD, …) sets; throws
`ArgumentException` on excess precision. Always convert amounts through it.

## PCI posture
This service never touches raw card numbers — the PAN is tokenized in Stripe's browser
iframe; only `pm_...` ids reach the backend. Keep it that way (SAQ A–eligible). Never add an
endpoint or DTO that accepts a raw card number, and never log/store PAN or put it in metadata.
Webhook PaymentIntent objects contain only `last4`/brand, never the full PAN.

## Testing
Integration tests via `WebApplicationFactory<Program>`; replace real services with fakes by
`services.RemoveAll<T>()` + `AddSingleton(fake)` and set `Stripe:SecretKey` to a dummy. Stripe
is never actually called. See `PaymentEndpointsTests` and `WebhookEndpointsTests`.
