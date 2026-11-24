using Stripe;
using StripeApiWrapper.Api.Contracts;

namespace StripeApiWrapper.Api.Services;

public interface IStripeCustomerService
{
    // Looks up an existing Stripe Customer by email and reuses it (updating its
    // details), else creates one. Returns the Stripe Customer (with its cus_ id).
    Task<Customer> GetOrCreateAsync(CustomerInfo info, CancellationToken cancellationToken);
}
