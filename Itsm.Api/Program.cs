using Itsm.Api;
using Itsm.Api.Endpoints;
using Itsm.Api.Hubs;
using Itsm.Api.Services;
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
        builder.Services.AddSignalR();
        builder.Services.AddSingleton<AgentLogService>();

        builder.Services.ConfigureHttpJsonOptions(options =>
            options.SerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter()));

        builder.Services.AddCors(options =>
        {
            options.AddPolicy("dev", policy =>
                policy.SetIsOriginAllowed(_ => true).AllowAnyMethod().AllowAnyHeader().AllowCredentials());
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

        app.MapHub<AgentHub>("/hubs/agent");

        app.MapComputerEndpoints();
        app.MapDiskUsageEndpoints();
        app.MapAgentEndpoints();

        app.Run();
    }
}
