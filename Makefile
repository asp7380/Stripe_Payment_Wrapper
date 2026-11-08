# Makefile for .NET project
.PHONY: start-stripe register-apple-pay-domain docker-status docker-down docker-recreate docker-clean

# The Docker Compose stack (Postgres + Strapi + this API + the Next.js
# frontend) is defined in the sibling `commerce-infrastructure` repo. Compose is
# run from that directory via `cd` so its .env file and relative build contexts
# resolve.
COMPOSE_DIR := ../commerce-infrastructure

start-stripe:
	dotnet clean
	dotnet run --project ./src/StripeApiWrapper.Api

# One-time Apple Pay domain registration. Stripe handles Apple merchant
# validation for you (no Apple Merchant ID / CSR needed); you just register
# every web domain and subdomain that shows the Apple Pay button, in both
# live mode and test/sandbox. Run once per domain, e.g.:
#   make register-apple-pay-domain STRIPE_SECRET_KEY=sk_live_... DOMAIN=example.com
# For local testing, register the HTTPS host from `stripe listen`/ngrok.
register-apple-pay-domain:
	@test -n "$(STRIPE_SECRET_KEY)" || (echo "STRIPE_SECRET_KEY is required" && exit 1)
	@test -n "$(DOMAIN)" || (echo "DOMAIN is required" && exit 1)
	curl https://api.stripe.com/v1/payment_method_domains \
	  -u "$(STRIPE_SECRET_KEY):" \
	  -d "domain_name=$(DOMAIN)"

# Show the status of the compose stack's containers (running + their health).
docker-status:
	cd $(COMPOSE_DIR) && docker compose ps

# Stop and remove the stack's containers and network. Named volumes
# (postgres-data, strapi-uploads) are preserved, so no data is lost.
docker-down:
	cd $(COMPOSE_DIR) && docker compose down

# Rebuild images and (re)create the containers, running detached in the
# background. Recreates any container whose image or config changed.
docker-recreate:
	cd $(COMPOSE_DIR) && docker compose up -d --build

# DESTRUCTIVE: stop/remove the containers AND delete the named volumes
# (postgres-data and strapi-uploads). This permanently wipes the database and
# all uploaded media - there is no undo. Strapi's first-run admin setup and any
# seeded content will need to be recreated afterward.
#docker-clean:
#	cd $(COMPOSE_DIR) && docker compose down -v
