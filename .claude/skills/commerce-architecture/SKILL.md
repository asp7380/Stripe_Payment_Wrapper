---
name: commerce-architecture
description: Multi-repo layout and git boundaries for the Stripe payment service and its sibling Strapi/Next.js commerce stack. Use when orienting in this project, locating code across repos, deciding where a change lives, or doing any git/branch/commit work — the repo boundaries are non-obvious and easy to get wrong.
---

# Commerce stack architecture & git layout

This "project" spans **three separate concerns across directories that are NOT one repo**.
Getting the boundaries wrong wastes time (e.g. trying to branch/commit a file that no repo
tracks), so check here first.

## Directory & git-repo map

| Path | What it is | Git repo? |
|------|-----------|-----------|
| `C:\git\stripe-payment-processor` | .NET 10 Stripe payment microservice (this repo) | **Yes** (own repo) |
| `C:\git\strapi` | Orchestration root: holds `docker-compose.yml`, `.env`, `.env.example`, `.vscode` | **NO** — not a git repo, and no parent is either |
| `C:\git\strapi\commerce-frontend` | Next.js 16 App Router storefront (BFF + Stripe Elements UI) | **Yes** (own repo, nested) |
| `C:\git\strapi\commerce-cms` | Strapi 5 CMS (products, orders) | **Yes** (own repo, nested) |

**Critical gotcha:** `C:\git\strapi` is *not* under version control (it only has an orphan
`.gitignore` containing `.env`). So `docker-compose.yml` and the `.env*` files there cannot
be put on a branch or into an MR as-is — they're untracked everywhere. Confirm with
`git rev-parse --show-toplevel` from that dir (it errors). Don't assume a `.gitignore`
implies a repo.

When committing changes that span repos, operate in each repo separately:
- backend code → `stripe-payment-processor`
- storefront/BFF code → `commerce-frontend`
- CMS schema/config → `commerce-cms`
- compose/orchestration → currently untracked (flag it; don't silently leave it out of history)

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
The explicit goal is to keep the payment service **independent of Strapi** and eventually
split the Strapi CMS out of the `strapi` monolith directory into its own deployable service.
Prefer designs that don't couple the .NET service to Strapi (the webhook callback URL is a
deliberate example).

See also: `run-payment-stack` (how to run/test) and `stripe-conventions` (code patterns).
