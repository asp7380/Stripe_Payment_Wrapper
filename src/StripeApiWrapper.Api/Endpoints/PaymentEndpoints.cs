using Stripe;
using StripeApiWrapper.Api.Contracts;
using StripeApiWrapper.Api.Services;

namespace StripeApiWrapper.Api.Endpoints;

public static class PaymentEndpoints
{
    public static void MapPaymentEndpoints(this WebApplication app)
    {
        app.MapPost("/api/payments", async (
            CreatePaymentRequest request,
            IPaymentService paymentService,
            CancellationToken cancellationToken) =>
        {
            if (request.Amount <= 0)
            {
                return Results.BadRequest("Amount must be greater than zero.");
            }

            if (string.IsNullOrWhiteSpace(request.Currency))
            {
                return Results.BadRequest("Currency is required.");
            }

            if (string.IsNullOrWhiteSpace(request.PaymentMethodId))
            {
                return Results.BadRequest("PaymentMethodId is required.");
            }

            try
            {
                var response = await paymentService.CreatePaymentAsync(request, cancellationToken);
                return Results.Ok(response);
            }
            catch (StripeException ex)
            {
                return Results.Problem(
                    detail: ex.StripeError?.Message ?? ex.Message,
                    statusCode: StatusCodes.Status400BadRequest);
            }
            catch (ArgumentException ex)
            {
                return Results.BadRequest(ex.Message);
            }
        });
    }
}
