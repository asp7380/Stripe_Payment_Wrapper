using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Serilog;
using Serilog.Events;
using Stripe;
using StripeApiWrapper.Api.Data;
using StripeApiWrapper.Api.Endpoints;
using StripeApiWrapper.Api.Options;
using StripeApiWrapper.Api.Services;

var builder = WebApplication.CreateBuilder(args);

// Serilog as the backend for ILogger<T>. Console gets everything; errors (and
// above) are also written to a NEW per-day folder: logs/<yyyy-MM-dd>/errors.log
// (via Sinks.Map keyed on the event date), so each day's errors are isolated.
builder.Host.UseSerilog((context, loggerConfig) => loggerConfig
    .MinimumLevel.Information()
    .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    // Only Error+ events reach the Map (via this sub-logger), so a dated folder
    // logs/<yyyy-MM-dd>/errors.log is created ONLY on days that actually have an
    // error - quiet days leave no empty folders.
    .WriteTo.Logger(errorLog => errorLog
        .MinimumLevel.Error()
        .WriteTo.Map(
            evt => evt.Timestamp.ToLocalTime().ToString("yyyy-MM-dd"),
            (date, sink) => sink.File(
                Path.Combine("logs", date, "errors.log"),
                shared: true))));

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
