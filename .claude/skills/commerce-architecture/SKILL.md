---
name: commerce-architecture
description: Polyrepo layout for the Stripe payment service and its sibling Next.js/Strapi commerce stack. Use when orienting in this project, locating code across repos, deciding where a change lives, or doing any git/branch/commit work — the work spans four separate repos.
---

# Commerce stack architecture & git layout

This project is a **polyrepo microservices stack**: four independent git repos checked out
as siblings under `C:\git`. When a change spans concerns, you commit in each repo separately —
check here first to know which repo owns what.

## Directory & git-repo map

All four are top-level siblings under `C:\git`, each its own git repo (no umbrella/parent repo):

| Path | What it is | Git repo? |
|------|-----------|-----------|
| `C:\git\stripe-payment-processor` | .NET 10 Stripe payment microservice (this repo) | **Yes** |
| `C:\git\commerce-infrastructure` | Orchestration: `docker-compose.yml`, `.env*`, `Makefile`, `README` | **Yes** |
| `C:\git\commerce-frontend` | Next.js 16 App Router storefront (BFF + Stripe Elements UI) | **Yes** |
| `C:\git\commerce-cms` | Strapi 5 CMS (products, orders) | **Yes** |

The stack runs from **`commerce-infrastructure`** — its `docker-compose.yml` build contexts
point at the sibling repos (`../commerce-cms`, `../commerce-frontend`,
`../stripe-payment-processor`). The real `.env` lives there and is gitignored; `.env.example`
is committed.

When committing changes that span repos, operate in each repo separately:
- backend code → `stripe-payment-processor`
- storefront/BFF code → `commerce-frontend`
- CMS schema/config → `commerce-cms`
- compose / orchestration / shared env → `commerce-infrastructure`

> Historical note: these repos previously lived nested under a non-git `C:\git\strapi`
> directory whose `docker-compose.yml`/`.env` were untracked. That was flattened into the
> sibling layout above, and the orchestration now lives in the `commerce-infrastructure` repo.

## End-to-end payment flow (who does what)

1. **Storefront** (`commerce-frontend`) `src/components/payment/PaymentForm.tsx` collects the
   card via Stripe `CardElement` and wallets via `PaymentRequestButtonElement`; tokenizes
   client-side into a `pm_...` PaymentMethod.
2. **BFF proxy** `commerce-frontend/src/app/api/payments/route.ts` re-derives price/currency
   from Strapi (client never names its own price), then POSTs to the .NET service. It sends
   `metadata: { productSlug, quantity }` for webhook fulfillment.
3. **Payment service** (`stripe-payment-processor`) `POST /api/payments` creates **and
   confirms** a PaymentIntent, returns `{ paymentIntentId, status, clientSecret }`.
4. **Fulfillment**: on immediate success the BFF records the order to Strapi; 3-D Secure is
   finished client-side then recorded via `commerce-frontend/src/app/api/orders/route.ts`.
   The .NET **Stripe webhook** (`POST /api/webhooks/stripe`) is the browser-independent
   backstop — it forwards to a configurable callback (the BFF `/api/orders`), keeping the
   payment service Strapi-agnostic. `createOrder` dedupes on `stripePaymentIntentId`, so the
   browser path and the webhook path are idempotent.

## Running direction / future intent
The payment service is intentionally **independent of Strapi** — e.g. the webhook forwards to
a configurable callback URL rather than talking to Strapi directly. Prefer designs that keep
the .NET service decoupled from Strapi. (The directory/repo split into the four siblings above
is already done; this is now about runtime/code coupling, not folder layout.)

See also: `run-payment-stack` (how to run/test) and `stripe-conventions` (code patterns).
