using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace StripeApiWrapper.Api.Data;

// Design-time factory so `dotnet ef` can build the model and generate migrations
// without running the app or opening a real connection. The connection string
// here is a placeholder used only to configure the Npgsql provider at design
// time; migrations are generated from the model, not the database.
public sealed class StripeServiceDbContextFactory : IDesignTimeDbContextFactory<StripeServiceDbContext>
{
    public StripeServiceDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<StripeServiceDbContext>()
            .UseNpgsql("Host=localhost;Database=StripeService;Username=postgres;Password=postgres")
            .Options;

        return new StripeServiceDbContext(options);
    }
}
