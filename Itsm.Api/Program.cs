using Itsm.Api;
using Itsm.Api.Endpoints;
using Npgsql;

namespace Itsm.Api;

public class Program
{
    public static void Main(string[] args)
    {
#pragma warning disable CS0618 // GlobalTypeMapper is obsolete but needed for Aspire integration
        NpgsqlConnection.GlobalTypeMapper.EnableDynamicJson();
#pragma warning restore CS0618

        var builder = WebApplication.CreateBuilder(args);

        builder.AddServiceDefaults();
        builder.AddNpgsqlDbContext<ItsmDbContext>("itsmdb");

        builder.Services.AddAuthorization();
        builder.Services.AddOpenApi();

        builder.Services.AddCors(options =>
        {
            options.AddPolicy("dev", policy =>
                policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());
        });

        var app = builder.Build();

        if (app.Environment.IsDevelopment())
        {
            app.MapOpenApi();
            app.UseCors("dev");
        }

        app.UseHttpsRedirection();
        app.UseAuthorization();

        app.MigrateDatabase();

        app.MapComputerEndpoints();
        app.MapDiskUsageEndpoints();

        app.Run();
    }
}
