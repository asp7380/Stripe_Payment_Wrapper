namespace StripeApiWrapper.Api.Contracts;

// Customer + shipping details captured at checkout. No card/PCI data.
public sealed record CustomerInfo(
    string FirstName,
    string LastName,
    string Email,
    string? Phone,
    ShippingAddress Shipping);

public sealed record ShippingAddress(
    string Line1,
    string? Line2,
    string City,
    string State,
    string PostalCode,
    string Country);
