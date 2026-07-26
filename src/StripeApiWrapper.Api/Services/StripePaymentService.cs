using Stripe;
using StripeApiWrapper.Api.Contracts;

namespace StripeApiWrapper.Api.Services;

public sealed class StripePaymentService(PaymentIntentService paymentIntentService) : IPaymentService
{
    public async Task<CreatePaymentResponse> CreatePaymentAsync(CreatePaymentRequest request, CancellationToken cancellationToken)
    {
        var options = new PaymentIntentCreateOptions
        {
            Amount = CurrencyMinorUnitConverter.ToMinorUnits(request.Amount, request.Currency),
            Currency = request.Currency,
            PaymentMethod = request.PaymentMethodId,
            Description = request.Description,
            Confirm = true,
            AutomaticPaymentMethods = new PaymentIntentAutomaticPaymentMethodsOptions
            {
                Enabled = true,
                AllowRedirects = "never",
            },
        };

        var intent = await paymentIntentService.CreateAsync(options, cancellationToken: cancellationToken);

        return new CreatePaymentResponse(intent.Id, intent.Status, intent.ClientSecret);
    }
}
