using Stripe;
using StripeApiWrapper.Api.Contracts;

namespace StripeApiWrapper.Api.Services;

public sealed class StripePaymentService(
    PaymentIntentService paymentIntentService,
    IStripeCustomerService stripeCustomerService) : IPaymentService
{
    public async Task<CreatePaymentResponse> CreatePaymentAsync(CreatePaymentRequest request, CancellationToken cancellationToken)
    {
        var metadata = request.Metadata is null
            ? new Dictionary<string, string>()
            : new Dictionary<string, string>(request.Metadata);

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

        if (request.Customer is { } customerInfo)
        {
            var customer = await stripeCustomerService.GetOrCreateAsync(customerInfo, cancellationToken);

            options.Customer = customer.Id;
            options.ReceiptEmail = customerInfo.Email;
            options.Shipping = new ChargeShippingOptions
            {
                Name = $"{customerInfo.FirstName} {customerInfo.LastName}".Trim(),
                Phone = customerInfo.Phone,
                Address = new AddressOptions
                {
                    Line1 = customerInfo.Shipping.Line1,
                    Line2 = customerInfo.Shipping.Line2,
                    City = customerInfo.Shipping.City,
                    State = customerInfo.Shipping.State,
                    PostalCode = customerInfo.Shipping.PostalCode,
                    Country = customerInfo.Shipping.Country,
                },
            };

            // Carry the structured customer fields in metadata so the webhook can
            // upsert the local Customer/Address on success (Stripe's single Name
            // field can't be split back into first/last). No card/PCI data here.
            AddCustomerMetadata(metadata, customerInfo, customer.Id);
        }

        options.Metadata = metadata.Count == 0 ? null : metadata;

        var intent = await paymentIntentService.CreateAsync(options, cancellationToken: cancellationToken);

        return new CreatePaymentResponse(intent.Id, intent.Status, intent.ClientSecret);
    }

    private static void AddCustomerMetadata(Dictionary<string, string> metadata, CustomerInfo customer, string stripeCustomerId)
    {
        metadata["customerStripeId"] = stripeCustomerId;
        metadata["customerFirstName"] = customer.FirstName;
        metadata["customerLastName"] = customer.LastName;
        metadata["customerEmail"] = customer.Email;
        if (!string.IsNullOrWhiteSpace(customer.Phone))
        {
            metadata["customerPhone"] = customer.Phone;
        }

        metadata["shipLine1"] = customer.Shipping.Line1;
        if (!string.IsNullOrWhiteSpace(customer.Shipping.Line2))
        {
            metadata["shipLine2"] = customer.Shipping.Line2;
        }

        metadata["shipCity"] = customer.Shipping.City;
        metadata["shipState"] = customer.Shipping.State;
        metadata["shipPostalCode"] = customer.Shipping.PostalCode;
        metadata["shipCountry"] = customer.Shipping.Country;
    }
}
