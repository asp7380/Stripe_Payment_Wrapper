# Makefile for .NET project
.PHONY: start-stripe docker-status docker-down docker-recreate docker-clean

# The Docker Compose stack (Postgres + Strapi + this API + the Next.js
# frontend) is defined in the sibling `strapi` repo. Compose is run from that
# directory via `cd` so its .env file and relative build contexts resolve.
COMPOSE_DIR := ../strapi

start-stripe:
	dotnet clean
	dotnet run --project ./src/StripeApiWrapper.Api

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
