using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Stripe;
using StripeApiWrapper.Api.Contracts;
using StripeApiWrapper.Api.Services;

namespace StripeApiWrapper.Api.Tests;

public class PaymentEndpointsTests
{
    private static WebApplicationFactory<Program> CreateFactory(IPaymentService fakePaymentService)
    {
        return new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseSetting("Stripe:SecretKey", "sk_test_not_a_real_key");

            builder.ConfigureServices(services =>
            {
                services.RemoveAll<IPaymentService>();
                services.AddSingleton(fakePaymentService);
            });
        });
    }

    [Fact]
    public async Task PostPayments_ReturnsOk_WhenPaymentSucceeds()
    {
        var expected = new CreatePaymentResponse("pi_123", "succeeded", "pi_123_secret_abc");
        await using var factory = CreateFactory(new FakePaymentService(_ => expected));
        using var client = factory.CreateClient();

        var request = new CreatePaymentRequest(10.00m, "usd", "pm_card_visa", "Test payment");
        var response = await client.PostAsJsonAsync("/api/payments", request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<CreatePaymentResponse>();
        Assert.Equal(expected, body);
    }

    [Fact]
    public async Task PostPayments_ReturnsBadRequest_WhenAmountIsNotPositive()
    {
        await using var factory = CreateFactory(new FakePaymentService(_ => throw new InvalidOperationException("Should not be called")));
        using var client = factory.CreateClient();

        var request = new CreatePaymentRequest(0m, "usd", "pm_card_visa", null);
        var response = await client.PostAsJsonAsync("/api/payments", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task PostPayments_ReturnsBadRequest_WhenAmountHasTooMuchPrecision()
    {
        await using var factory = CreateFactory(new FakePaymentService(_ => throw new ArgumentException("Amount 10.005 has more decimal precision than currency 'usd' supports.")));
        using var client = factory.CreateClient();

        var request = new CreatePaymentRequest(10.005m, "usd", "pm_card_visa", null);
        var response = await client.PostAsJsonAsync("/api/payments", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task PostPayments_ReturnsProblem_WhenStripeThrows()
    {
        await using var factory = CreateFactory(new FakePaymentService(_ => throw new StripeException("Your card was declined.")));
        using var client = factory.CreateClient();

        var request = new CreatePaymentRequest(10.00m, "usd", "pm_card_chargeDeclined", null);
        var response = await client.PostAsJsonAsync("/api/payments", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    private sealed class FakePaymentService(Func<CreatePaymentRequest, CreatePaymentResponse> handler) : IPaymentService
    {
        public Task<CreatePaymentResponse> CreatePaymentAsync(CreatePaymentRequest request, CancellationToken cancellationToken)
            => Task.FromResult(handler(request));
    }
}
