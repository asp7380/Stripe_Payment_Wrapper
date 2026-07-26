using Microsoft.Extensions.Options;
using Stripe;
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

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapGet("/", () => "Hello World!");
app.MapPaymentEndpoints();

app.Run();

public partial class Program;
