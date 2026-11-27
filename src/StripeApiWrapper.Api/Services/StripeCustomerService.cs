using Stripe;
using StripeApiWrapper.Api.Contracts;

namespace StripeApiWrapper.Api.Services;

public sealed class StripeCustomerService(CustomerService customerService) : IStripeCustomerService
{
    public async Task<Customer> GetOrCreateAsync(CustomerInfo info, CancellationToken cancellationToken)
    {
        var name = $"{info.FirstName} {info.LastName}".Trim();
        var address = new AddressOptions
        {
            Line1 = info.Shipping.Line1,
            Line2 = info.Shipping.Line2,
            City = info.Shipping.City,
            State = info.Shipping.State,
            PostalCode = info.Shipping.PostalCode,
            Country = info.Shipping.Country,
        };
        // List-by-email is an exact, immediately-consistent match (unlike the
        // Search API, which has indexing lag), so it's safe for dedup. Reuse the
        // most recent match if one exists; otherwise create a new Customer.
        var existing = await customerService.ListAsync(
            new CustomerListOptions { Email = info.Email, Limit = 1 },
            cancellationToken: cancellationToken);

        if (existing.Data.Count > 0)
        {
            return await customerService.UpdateAsync(
                existing.Data[0].Id,
                new CustomerUpdateOptions
                {
                    Name = name,
                    Phone = info.Phone,
                    Address = address,
                },
                cancellationToken: cancellationToken);
        }

        return await customerService.CreateAsync(
            new CustomerCreateOptions
            {
                Email = info.Email,
                Name = name,
                Phone = info.Phone,
                Address = address,
            },
            cancellationToken: cancellationToken);
    }
}
