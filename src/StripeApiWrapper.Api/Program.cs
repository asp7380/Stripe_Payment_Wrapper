using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Stripe;
using StripeApiWrapper.Api.Data;
using StripeApiWrapper.Api.Endpoints;
using StripeApiWrapper.Api.Options;
using StripeApiWrapper.Api.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddOptions<StripeOptions>()
    .Bind(builder.Configuration.GetSection("Stripe"))
    .ValidateDataAnnotations()
    .ValidateOnStart();

builder.Services.AddSingleton<IStripeClient>(sp =>
{
    var options = sp.GetRequiredService<IOptions<StripeOptions>>().Value;
    return new StripeClient(options.SecretKey);
});

builder.Services.AddScoped(sp => new PaymentIntentService(sp.GetRequiredService<IStripeClient>()));

builder.Services.Scan(scan => scan
    .FromAssemblyOf<Program>()
    .AddClasses(classes => classes.AssignableTo<IPaymentService>())
    .AsImplementedInterfaces()
    .WithScopedLifetime());

builder.Services.AddScoped<IStripeEventReader, StripeEventReader>();
builder.Services.AddHttpClient<IFulfillmentNotifier, HttpFulfillmentNotifier>();

// The API owns a StripeService Postgres DB for local Customer/Address records.
var stripeServiceConnection = builder.Configuration.GetConnectionString("StripeService");
builder.Services.AddDbContext<StripeServiceDbContext>(options =>
    options.UseNpgsql(stripeServiceConnection));

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Create the StripeService database (if missing) and apply migrations on
// startup. Guarded by a configured connection string so the in-process tests
// (which don't set one) skip all database access.
if (!string.IsNullOrWhiteSpace(stripeServiceConnection))
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<StripeServiceDbContext>();
    db.Database.Migrate();
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapGet("/", () => "Hello World!");
app.MapPaymentEndpoints();
app.MapWebhookEndpoints();

app.Run();

public partial class Program;
