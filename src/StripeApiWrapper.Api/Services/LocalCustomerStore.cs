using Microsoft.EntityFrameworkCore;
using StripeApiWrapper.Api.Constants;
using StripeApiWrapper.Api.Data;
using StripeApiWrapper.Api.Data.Entities;

namespace StripeApiWrapper.Api.Services;

// `Customer`/`Address` below refer to the local EF entities. The only Stripe type
// used here (PaymentIntent) is fully qualified to avoid the name clash.
public sealed class LocalCustomerStore(StripeServiceDbContext db) : ILocalCustomerStore
{
    private const string ShippingLabel = "shipping";

    public async Task UpsertFromPaymentAsync(Stripe.PaymentIntent paymentIntent, CancellationToken cancellationToken)
    {
        var metadata = paymentIntent.Metadata;
        if (metadata is null
            || !metadata.TryGetValue(PaymentMetadataKeys.CustomerEmail, out var email)
            || string.IsNullOrWhiteSpace(email))
        {
            return; // this payment carried no customer info
        }

        var stripeCustomerId = paymentIntent.CustomerId ?? metadata.GetValueOrDefault(PaymentMetadataKeys.CustomerStripeId);
        var now = DateTimeOffset.UtcNow;

        // Upsert by email (dedup mirrors the Stripe-side look-up-or-create).
        var customer = await db.Customers
            .Include(c => c.Addresses)
            .FirstOrDefaultAsync(c => c.Email == email, cancellationToken);

        if (customer is null)
        {
            customer = new Customer { Email = email, CreatedAt = now };
            db.Customers.Add(customer);
        }

        customer.FirstName = metadata.GetValueOrDefault(PaymentMetadataKeys.CustomerFirstName) ?? customer.FirstName;
        customer.LastName = metadata.GetValueOrDefault(PaymentMetadataKeys.CustomerLastName) ?? customer.LastName;
        customer.Phone = metadata.GetValueOrDefault(PaymentMetadataKeys.CustomerPhone);
        customer.StripeCustomerId = stripeCustomerId;
        customer.StripeSyncedAt = now;
        customer.UpdatedAt = now;

        // Upsert the single "shipping" address for this customer.
        if (metadata.TryGetValue(PaymentMetadataKeys.ShipLine1, out var line1) && !string.IsNullOrWhiteSpace(line1))
        {
            var address = customer.Addresses.FirstOrDefault(a => a.Label == ShippingLabel);
            if (address is null)
            {
                address = new Address { Label = ShippingLabel, CreatedAt = now };
                customer.Addresses.Add(address);
            }

            address.Line1 = line1;
            address.Line2 = metadata.GetValueOrDefault(PaymentMetadataKeys.ShipLine2);
            address.City = metadata.GetValueOrDefault(PaymentMetadataKeys.ShipCity) ?? string.Empty;
            address.State = metadata.GetValueOrDefault(PaymentMetadataKeys.ShipState) ?? string.Empty;
            address.PostalCode = metadata.GetValueOrDefault(PaymentMetadataKeys.ShipPostalCode) ?? string.Empty;
            address.Country = metadata.GetValueOrDefault(PaymentMetadataKeys.ShipCountry) ?? string.Empty;
            address.StripeCustomerId = stripeCustomerId;
            address.StripeSyncedAt = now;
            address.UpdatedAt = now;
        }

        await db.SaveChangesAsync(cancellationToken);
    }
}
