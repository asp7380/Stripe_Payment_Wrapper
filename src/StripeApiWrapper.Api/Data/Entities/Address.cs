namespace StripeApiWrapper.Api.Data.Entities;

// A postal address belonging to a Customer (one-to-many: a customer can have
// several). No card/PCI data.
public class Address
{
    public Guid Id { get; set; }

    public string Line1 { get; set; } = string.Empty;

    public string? Line2 { get; set; }

    public string City { get; set; } = string.Empty;

    public string State { get; set; } = string.Empty;

    public string PostalCode { get; set; } = string.Empty;

    // ISO 3166-1 alpha-2 country code (e.g. "US").
    public string Country { get; set; } = string.Empty;

    // Optional label, e.g. "shipping" or "billing".
    public string? Label { get; set; }

    // Stripe has no id for an address (they're embedded in the customer/payment),
    // so this holds the cus_... this address is synced under.
    public string? StripeCustomerId { get; set; }

    // When this address was last pushed to Stripe.
    public DateTimeOffset? StripeSyncedAt { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }

    // Owning customer (the "many" side's foreign key + navigation).
    public Guid CustomerId { get; set; }

    public Customer Customer { get; set; } = null!;
}
