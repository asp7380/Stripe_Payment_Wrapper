using Microsoft.EntityFrameworkCore;
using StripeApiWrapper.Api.Data.Entities;

namespace StripeApiWrapper.Api.Data;

public class StripeServiceDbContext(DbContextOptions<StripeServiceDbContext> options)
    : DbContext(options)
{
    public DbSet<Customer> Customers => Set<Customer>();

    public DbSet<Address> Addresses => Set<Address>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Customer>(entity =>
        {
            entity.HasKey(c => c.Id);
            // Postgres 13+ (we run 16) has gen_random_uuid() built in, so keys
            // are generated database-side.
            entity.Property(c => c.Id).HasDefaultValueSql("gen_random_uuid()");

            entity.Property(c => c.FirstName).IsRequired().HasMaxLength(100);
            entity.Property(c => c.LastName).IsRequired().HasMaxLength(100);
            entity.Property(c => c.Email).IsRequired().HasMaxLength(320);
            entity.Property(c => c.Phone).HasMaxLength(32);
            entity.Property(c => c.StripeCustomerId).HasMaxLength(64);

            entity.HasIndex(c => c.Email);
            entity.HasIndex(c => c.StripeCustomerId);

            entity.HasMany(c => c.Addresses)
                .WithOne(a => a.Customer)
                .HasForeignKey(a => a.CustomerId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Address>(entity =>
        {
            entity.HasKey(a => a.Id);
            entity.Property(a => a.Id).HasDefaultValueSql("gen_random_uuid()");

            entity.Property(a => a.Line1).IsRequired().HasMaxLength(200);
            entity.Property(a => a.Line2).HasMaxLength(200);
            entity.Property(a => a.City).IsRequired().HasMaxLength(100);
            entity.Property(a => a.State).IsRequired().HasMaxLength(100);
            entity.Property(a => a.PostalCode).IsRequired().HasMaxLength(20);
            entity.Property(a => a.Country).IsRequired().HasMaxLength(2);
            entity.Property(a => a.Label).HasMaxLength(32);
        });
    }
}
