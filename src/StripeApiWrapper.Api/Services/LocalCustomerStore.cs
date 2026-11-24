using Microsoft.EntityFrameworkCore;
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
            || !metadata.TryGetValue("customerEmail", out var email)
            || string.IsNullOrWhiteSpace(email))
        {
            return; // this payment carried no customer info
        }

        var stripeCustomerId = paymentIntent.CustomerId ?? metadata.GetValueOrDefault("customerStripeId");
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

        customer.FirstName = metadata.GetValueOrDefault("customerFirstName") ?? customer.FirstName;
        customer.LastName = metadata.GetValueOrDefault("customerLastName") ?? customer.LastName;
        customer.Phone = metadata.GetValueOrDefault("customerPhone");
        customer.StripeCustomerId = stripeCustomerId;
        customer.StripeSyncedAt = now;
        customer.UpdatedAt = now;

        // Upsert the single "shipping" address for this customer.
        if (metadata.TryGetValue("shipLine1", out var line1) && !string.IsNullOrWhiteSpace(line1))
        {
            var address = customer.Addresses.FirstOrDefault(a => a.Label == ShippingLabel);
            if (address is null)
            {
                address = new Address { Label = ShippingLabel, CreatedAt = now };
                customer.Addresses.Add(address);
            }

            address.Line1 = line1;
            address.Line2 = metadata.GetValueOrDefault("shipLine2");
            address.City = metadata.GetValueOrDefault("shipCity") ?? string.Empty;
            address.State = metadata.GetValueOrDefault("shipState") ?? string.Empty;
            address.PostalCode = metadata.GetValueOrDefault("shipPostalCode") ?? string.Empty;
            address.Country = metadata.GetValueOrDefault("shipCountry") ?? string.Empty;
            address.StripeCustomerId = stripeCustomerId;
            address.StripeSyncedAt = now;
            address.UpdatedAt = now;
        }

        await db.SaveChangesAsync(cancellationToken);
    }
}
