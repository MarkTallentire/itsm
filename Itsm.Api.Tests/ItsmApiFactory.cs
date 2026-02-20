using System.Text.Json;
using Itsm.Common.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Itsm.Api.Tests;

public class ItsmApiFactory : WebApplicationFactory<Program>
{
    private readonly string _dbName = Guid.NewGuid().ToString();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");

        builder.ConfigureServices(services =>
        {
            // Remove everything related to DbContext so we can replace with InMemory
            var toRemove = services.Where(d =>
                d.ServiceType == typeof(DbContextOptions<ItsmDbContext>) ||
                d.ServiceType == typeof(ItsmDbContext) ||
                d.ServiceType.FullName?.Contains("DbContext") == true ||
                d.ServiceType.FullName?.Contains("Npgsql") == true ||
                d.ImplementationType?.FullName?.Contains("Npgsql") == true ||
                d.ImplementationFactory?.Method.ReturnType.FullName?.Contains("Npgsql") == true
            ).ToList();

            foreach (var d in toRemove)
                services.Remove(d);

            services.RemoveAll(typeof(DbContextOptions));
            services.RemoveAll(typeof(DbContextOptions<ItsmDbContext>));

            services.AddDbContext<TestItsmDbContext>(options =>
                options.UseInMemoryDatabase(_dbName));

            services.AddScoped<ItsmDbContext>(sp => sp.GetRequiredService<TestItsmDbContext>());
        });
    }
}

/// <summary>
/// Adds a value converter for DiskUsageSnapshot.Data so InMemory can handle the jsonb column.
/// </summary>
public class TestItsmDbContext : ItsmDbContext
{
    public TestItsmDbContext(DbContextOptions<TestItsmDbContext> options)
        : base(ChangeOptionsType(options)) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        var converter = new ValueConverter<DiskUsageSnapshot, string>(
            v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
            v => JsonSerializer.Deserialize<DiskUsageSnapshot>(v, (JsonSerializerOptions?)null)!);

        modelBuilder.Entity<DiskUsageRecord>()
            .Property(d => d.Data)
            .HasConversion(converter)
            .HasColumnType(null); // Clear the jsonb column type for InMemory
    }

    private static DbContextOptions<ItsmDbContext> ChangeOptionsType(
        DbContextOptions<TestItsmDbContext> options)
    {
        var builder = new DbContextOptionsBuilder<ItsmDbContext>();

        foreach (var ext in options.Extensions)
            ((IDbContextOptionsBuilderInfrastructure)builder).AddOrUpdateExtension(ext);

        return builder.Options;
    }
}
