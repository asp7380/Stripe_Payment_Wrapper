using System.Net;
using System.Net.Http.Json;
using System.Text;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Stripe;
using StripeApiWrapper.Api.Services;

namespace StripeApiWrapper.Api.Tests;

public class WebhookEndpointsTests
{
    private static WebApplicationFactory<Program> CreateFactory(
        IStripeEventReader eventReader,
        IFulfillmentNotifier fulfillmentNotifier)
    {
        return new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseSetting("Stripe:SecretKey", "sk_test_not_a_real_key");

            builder.ConfigureServices(services =>
            {
                services.RemoveAll<IStripeEventReader>();
                services.AddSingleton(eventReader);
                services.RemoveAll<IFulfillmentNotifier>();
                services.AddSingleton(fulfillmentNotifier);
            });
        });
    }

    private static async Task<HttpResponseMessage> PostWebhook(HttpClient client, string body)
    {
        using var message = new HttpRequestMessage(HttpMethod.Post, "/api/webhooks/stripe")
        {
            Content = new StringContent(body, Encoding.UTF8, "application/json"),
        };
        message.Headers.Add("Stripe-Signature", "t=1,v1=test-signature");
        return await client.SendAsync(message);
    }

    [Fact]
    public async Task PostWebhook_ReturnsBadRequest_WhenSignatureInvalid()
    {
        var notifier = new RecordingFulfillmentNotifier();
        var reader = new FakeEventReader((_, _) => throw new StripeException("Invalid signature."));
        await using var factory = CreateFactory(reader, notifier);
        using var client = factory.CreateClient();

        var response = await PostWebhook(client, "{}");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(0, notifier.CallCount);
    }

    [Fact]
    public async Task PostWebhook_InvokesFulfillment_OnPaymentIntentSucceeded()
    {
        var paymentIntent = new PaymentIntent { Id = "pi_123" };
        var stripeEvent = new Event
        {
            Type = "payment_intent.succeeded",
            Data = new EventData { Object = paymentIntent },
        };

        var notifier = new RecordingFulfillmentNotifier();
        var reader = new FakeEventReader((_, _) => stripeEvent);
        await using var factory = CreateFactory(reader, notifier);
        using var client = factory.CreateClient();

        var response = await PostWebhook(client, "{}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(1, notifier.CallCount);
        Assert.Equal("pi_123", notifier.Received?.Id);
    }

    [Fact]
    public async Task PostWebhook_IgnoresUnhandledEvents()
    {
        var stripeEvent = new Event
        {
            Type = "charge.updated",
            Data = new EventData { Object = new Charge { Id = "ch_1" } },
        };

        var notifier = new RecordingFulfillmentNotifier();
        var reader = new FakeEventReader((_, _) => stripeEvent);
        await using var factory = CreateFactory(reader, notifier);
        using var client = factory.CreateClient();

        var response = await PostWebhook(client, "{}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(0, notifier.CallCount);
    }

    [Fact]
    public async Task PostWebhook_Returns500_WhenFulfillmentThrows()
    {
        var stripeEvent = new Event
        {
            Type = "payment_intent.succeeded",
            Data = new EventData { Object = new PaymentIntent { Id = "pi_err" } },
        };

        var notifier = new RecordingFulfillmentNotifier(() => throw new HttpRequestException("callback down"));
        var reader = new FakeEventReader((_, _) => stripeEvent);
        await using var factory = CreateFactory(reader, notifier);
        using var client = factory.CreateClient();

        var response = await PostWebhook(client, "{}");

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
    }

    private sealed class FakeEventReader(Func<string, string, Event> handler) : IStripeEventReader
    {
        public Event Construct(string json, string signatureHeader) => handler(json, signatureHeader);
    }

    private sealed class RecordingFulfillmentNotifier(Action? onCall = null) : IFulfillmentNotifier
    {
        public PaymentIntent? Received { get; private set; }
        public int CallCount { get; private set; }

        public Task NotifyAsync(PaymentIntent paymentIntent, CancellationToken cancellationToken)
        {
            Received = paymentIntent;
            CallCount++;
            onCall?.Invoke();
            return Task.CompletedTask;
        }
    }
}
