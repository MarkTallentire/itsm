using System.Text.Json;
using System.Text.Json.Serialization;
using Itsm.Api.Hubs;
using Itsm.Api.Services;
using Itsm.Common.Models;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace Itsm.Api.Endpoints;

public static class AgentEndpoints
{
    public static void MapAgentEndpoints(this WebApplication app)
    {
        app.MapGet("/agents", async (ItsmDbContext db) =>
            await db.Agents.OrderBy(a => a.ComputerName).ToListAsync());

        app.MapGet("/agents/{hardwareUuid}", async (string hardwareUuid, ItsmDbContext db) =>
        {
            var agent = await db.Agents.FindAsync(hardwareUuid);
            return agent is null ? Results.NotFound() : Results.Ok(agent);
        });

        app.MapPost("/agents/{hardwareUuid}/request-update", async (
            string hardwareUuid,
            [Microsoft.AspNetCore.Mvc.FromBody] UpdateType updateType,
            IHubContext<AgentHub> hubContext) =>
        {
            var connectionId = AgentHub.GetConnectionId(hardwareUuid);
            if (connectionId is null)
                return Results.NotFound(new { error = "Agent is not connected" });

            await hubContext.Clients.Client(connectionId).SendAsync("RequestUpdate", updateType);
            return Results.Accepted();
        }).WithName("agents/request-update");

        app.MapGet("/agents/{hardwareUuid}/logs", (string hardwareUuid, AgentLogService logService) =>
            Results.Ok(logService.GetRecentLogs(hardwareUuid)));

        app.MapGet("/agents/{hardwareUuid}/logs/stream", async (
            string hardwareUuid,
            AgentLogService logService,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            httpContext.Response.ContentType = "text/event-stream";
            httpContext.Response.Headers.CacheControl = "no-cache";
            httpContext.Response.Headers.Connection = "keep-alive";

            var (channel, subscription) = logService.Subscribe(hardwareUuid);
            using var _ = subscription;

            await foreach (var entry in channel.Reader.ReadAllAsync(cancellationToken))
            {
                var json = JsonSerializer.Serialize(entry, JsonContext.Default.LogEntry);
                await httpContext.Response.WriteAsync($"data: {json}\n\n", cancellationToken);
                await httpContext.Response.Body.FlushAsync(cancellationToken);
            }
        });
    }
}

[System.Text.Json.Serialization.JsonSerializable(typeof(LogEntry))]
internal partial class JsonContext : JsonSerializerContext;
