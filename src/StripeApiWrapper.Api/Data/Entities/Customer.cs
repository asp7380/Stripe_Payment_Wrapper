namespace StripeApiWrapper.Api.Data.Entities;

// A local record of a customer, written alongside the Stripe Customer for
// reporting/other needs. Holds no card/PCI data.
public class Customer
{
    public Guid Id { get; set; }

    public string FirstName { get; set; } = string.Empty;

    public string LastName { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string? Phone { get; set; }

    // Links this local record to the Stripe Customer (cus_...). Null until the
    // Stripe Customer has been created/associated.
    public string? StripeCustomerId { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }

    // One customer can have many addresses (shipping, billing, ...).
    public ICollection<Address> Addresses { get; set; } = new List<Address>();
}
