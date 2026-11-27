namespace StripeApiWrapper.Api.Constants;

// Keys used in Stripe PaymentIntent metadata, shared between the writer
// (StripePaymentService) and the readers (LocalCustomerStore,
// HttpFulfillmentNotifier). These are internal identifiers that never cross to
// the client as a value domain, so they are const strings (not an enum).
public static class PaymentMetadataKeys
{
    public const string ProductSlug = "productSlug";
    public const string Quantity = "quantity";

    public const string CustomerStripeId = "customerStripeId";
    public const string CustomerFirstName = "customerFirstName";
    public const string CustomerLastName = "customerLastName";
    public const string CustomerEmail = "customerEmail";
    public const string CustomerPhone = "customerPhone";

    public const string ShipLine1 = "shipLine1";
    public const string ShipLine2 = "shipLine2";
    public const string ShipCity = "shipCity";
    public const string ShipState = "shipState";
    public const string ShipPostalCode = "shipPostalCode";
    public const string ShipCountry = "shipCountry";
}
