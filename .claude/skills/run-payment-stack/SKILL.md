---
name: run-payment-stack
description: How to run, and test the Stripe payment service and the full commerce stack (Postgres + Strapi + .NET API + Next.js). Use when asked to start/run the app, test payments, exercise the Stripe webhook locally, run the test suite, or set up Apple Pay domain registration.
---

# Run & test the payment stack

## Run the .NET payment service alone
From `C:\git\stripe-payment-processor`:
```
make start-stripe         # dotnet clean + dotnet run --project ./src/StripeApiWrapper.Api
```
Local URLs (from `launchSettings.json`): `http://localhost:5212` (http profile) and
`https://localhost:7184`. Swagger is served in Development.

The Stripe **secret key** is required at startup (`ValidateOnStart`). It's read from config
section `Stripe` → set via User Secrets (`UserSecretsId` is in the csproj) or env var
`Stripe__SecretKey`. Webhook-related keys are optional (service boots without them).

## Run the whole stack (Docker Compose)
Compose lives in the sibling **non-repo** dir `../strapi` (see `commerce-architecture`).
Makefile targets `cd` into it:
```
make docker-recreate      # docker compose up -d --build  (Postgres, Strapi, api, frontend)
make docker-status        # docker compose ps
make docker-down          # stop/remove containers; named volumes preserved
```
Published ports: Strapi `1337`, payment API `5212`→8080, frontend `3000`, Postgres `5432`.
`docker-clean` (commented out) is destructive — it deletes volumes (DB + uploads).

## Run backend tests
```
dotnet test               # xUnit; 19 tests. WebApplicationFactory<Program> integration style.
```
Tests fake dependencies via DI (`RemoveAll<T>()` + `AddSingleton(fake)`) and set a dummy
`Stripe:SecretKey`; Stripe is never actually called. Webhook tests fake `IStripeEventReader`
and `IFulfillmentNotifier` so no real signatures are needed.

## Test the Stripe webhook locally (Stripe CLI)
```
stripe login
stripe listen --forward-to localhost:5212/api/webhooks/stripe   # prints a whsec_ secret
# set that as Stripe__WebhookSecret (env or user-secrets), restart the service
stripe trigger payment_intent.succeeded
```
Expect: signature verified → `IFulfillmentNotifier` POSTs `{ productSlug, quantity,
paymentIntentId }` to `Stripe__FulfillmentCallbackUrl` → order upserted in Strapi (a repeat
trigger with the same PI is a dedupe no-op). Bad signature → 400; fulfillment failure → 500
(so Stripe retries).

## Test cards (test mode)
- `4242 4242 4242 4242` — succeeds, no auth.
- `4000 0025 0000 3155` — requires 3-D Secure (exercises the `requires_action` path).
- `4000 0000 0000 9995` — declined (`insufficient_funds`).
Any future expiry, any CVC, any postal code.

## Apple Pay domain registration (one-time, for the wallet button to appear)
Apple Pay on web needs the domain **registered with Stripe** (Google Pay does not). From
`stripe-payment-processor`:
```
make register-apple-pay-domain STRIPE_SECRET_KEY=sk_... DOMAIN=example.com
```
Register every domain/subdomain showing the button, in live + test. For local testing use
the HTTPS host from `stripe listen`/ngrok (localhost won't work). Then host the association
file at `commerce-frontend/public/.well-known/apple-developer-merchantid-domain-association`
(download it from the Stripe Dashboard — its contents can't be generated) and confirm via
Stripe's wallet test page.
