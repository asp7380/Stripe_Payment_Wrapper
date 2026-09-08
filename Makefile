# Makefile for .NET project
.PHONY: start-stripe

start-stripe:
	dotnet clean
	dotnet run --project ./src/StripeApiWrapper.Api