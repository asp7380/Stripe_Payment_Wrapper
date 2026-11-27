using Microsoft.EntityFrameworkCore;
using Stripe;
using StripeApiWrapper.Api.Data;
using StripeApiWrapper.Api.Services;

namespace StripeApiWrapper.Api.Tests;

public class LocalCustomerStoreTests
{
    private static StripeServiceDbContext NewContext() =>
        new(new DbContextOptionsBuilder<StripeServiceDbContext>()
            .UseInMemoryDatabase($"stripeservice-{Guid.NewGuid()}")
            .Options);

    private static PaymentIntent PaymentWithCustomer(
        string email, string firstName, string lastName, string line1 = "1 Main St") =>
        new()
        {
            Id = "pi_test",
            CustomerId = "cus_test",
            Metadata = new Dictionary<string, string>
            {
                ["customerStripeId"] = "cus_test",
                ["customerFirstName"] = firstName,
                ["customerLastName"] = lastName,
                ["customerEmail"] = email,
                ["customerPhone"] = "+15555550123",
                ["shipLine1"] = line1,
                ["shipCity"] = "Springfield",
                ["shipState"] = "IL",
                ["shipPostalCode"] = "62704",
                ["shipCountry"] = "US",
            },
        };

    [Fact]
    public async Task Upsert_InsertsNewCustomerAndShippingAddress()
    {
        await using var db = NewContext();
        var store = new LocalCustomerStore(db);

        await store.UpsertFromPaymentAsync(PaymentWithCustomer("jane@example.com", "Jane", "Doe"), default);

        var customer = await db.Customers.Include(c => c.Addresses).SingleAsync();
        Assert.Equal("Jane", customer.FirstName);
        Assert.Equal("Doe", customer.LastName);
        Assert.Equal("jane@example.com", customer.Email);
        Assert.Equal("cus_test", customer.StripeCustomerId);
        Assert.NotNull(customer.StripeSyncedAt);

        var address = Assert.Single(customer.Addresses);
        Assert.Equal("shipping", address.Label);
        Assert.Equal("1 Main St", address.Line1);
        Assert.Equal("62704", address.PostalCode);
        Assert.Equal("cus_test", address.StripeCustomerId);
    }

    [Fact]
    public async Task Upsert_UpdatesExistingCustomerByEmail_WithoutDuplicating()
    {
        await using var db = NewContext();
        var store = new LocalCustomerStore(db);

        await store.UpsertFromPaymentAsync(PaymentWithCustomer("jane@example.com", "Jane", "Doe"), default);
        await store.UpsertFromPaymentAsync(PaymentWithCustomer("jane@example.com", "Janet", "Smith", "2 New Rd"), default);

        var customer = await db.Customers.Include(c => c.Addresses).SingleAsync();
        Assert.Equal("Janet", customer.FirstName);
        Assert.Equal("Smith", customer.LastName);

        var address = Assert.Single(customer.Addresses);
        Assert.Equal("2 New Rd", address.Line1);
    }

    [Fact]
    public async Task Upsert_IsNoOp_WhenPaymentHasNoCustomerMetadata()
    {
        await using var db = NewContext();
        var store = new LocalCustomerStore(db);

        await store.UpsertFromPaymentAsync(new PaymentIntent { Id = "pi_1" }, default);

        Assert.Empty(await db.Customers.ToListAsync());
    }
}
