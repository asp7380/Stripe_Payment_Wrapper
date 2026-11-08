---
name: stripe-conventions
description: Code conventions for the .NET Stripe payment microservice (stripe-payment-processor) — DI registration, PaymentIntent/webhook/fulfillment patterns, the metadata contract, the StripeService EF Core database (Customer/Address), Serilog logging, currency handling, config, and PCI posture. Use when adding or changing payment endpoints, services, webhooks, the database, logging, or Stripe options in this repo.
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

## Data / StripeService DB (EF Core)
The API owns a Postgres database **`StripeService`** (EF Core 10, Npgsql) holding local
`Customer`/`Address` records for reporting — separate from Stripe (the payment source of truth)
and from Strapi's DB. All code under `Data/`.
- Entities `Data/Entities/{Customer,Address}.cs`: **Guid** PKs (`gen_random_uuid()` default),
  **one-to-many** (a customer has many addresses), Stripe-link fields `StripeCustomerId`
  (updatable, latest `cus_…`) + `StripeSyncedAt`. **No card/PCI data, ever.**
- `Data/StripeServiceDbContext.cs` sets constraints/indexes in `OnModelCreating`.
  `Data/StripeServiceDbContextFactory.cs` is a design-time factory so `dotnet ef` builds
  migrations without running the app.
- `Program.cs`: `AddDbContext<StripeServiceDbContext>(o => o.UseNpgsql(connString))` where
  `connString = Configuration.GetConnectionString("StripeService")`.
- **Startup migration** `db.Database.Migrate()` runs on boot **guarded by a non-empty connection
  string** — so `WebApplicationFactory<Program>` tests (which set none) skip all DB access and
  stay DB-free. Migrate also creates the database if it doesn't exist.
- Migrations live in `Data/Migrations/`; add with
  `dotnet ef migrations add <Name> --project src/StripeApiWrapper.Api/StripeApiWrapper.Api.csproj -o Data/Migrations`
  (`dotnet-ef` is a local tool in the manifest). Connection string: local via **user-secrets**,
  docker via the `ConnectionStrings__StripeService` env in commerce-infrastructure's compose.

## Logging (Serilog)
**Serilog** is the backend for `ILogger<T>` — app code keeps using plain `ILogger<T>` /
`logger.LogError(...)`; do **not** reference Serilog outside `Program.cs`. Configured once via
`builder.Host.UseSerilog(...)`: Console for all output (Information+); **Error+** is routed
through an Error-only sub-logger + `Serilog.Sinks.Map` to a **per-day folder**
`logs/<yyyy-MM-dd>/errors.log`. The Error-only sub-logger is deliberate — it means a dated folder
is created ONLY on days with a real error (no empty folders). In docker, `logs/` is the
`api-logs` named volume; read with `make logs-errors` / `make logs-errors-ls` (commerce-
infrastructure). `logs/` is gitignored.

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
