using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;
using Itsm.Api.Services;
using Itsm.Common.Models;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace Itsm.Api.Hubs;

public class AgentHub(IServiceScopeFactory scopeFactory, AgentLogService logService, ILogger<AgentHub> logger) : Hub
{
    // Maps hardwareUuid → connectionId
    private static readonly ConcurrentDictionary<string, string> ConnectedAgents = new();

    private static readonly string[] Adjectives =
    [
        "Swift", "Bold", "Calm", "Eager", "Brave", "Keen", "Deft", "Vivid",
        "Noble", "Agile", "Sleek", "Witty", "Plucky", "Nimble", "Steady",
        "Radiant", "Cosmic", "Silent", "Lucky", "Bright", "Frosty", "Golden"
    ];

    private static readonly string[] Nouns =
    [
        "Falcon", "Otter", "Panda", "Phoenix", "Raven", "Tiger", "Wolf",
        "Hawk", "Lynx", "Fox", "Badger", "Cobra", "Heron", "Osprey",
        "Jaguar", "Viper", "Mantis", "Condor", "Bison", "Crane", "Puma"
    ];

    private static string GenerateDisplayName(string seed)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(seed.ToLowerInvariant()));
        var hash = Math.Abs(BitConverter.ToInt32(bytes, 0));
        var adj = Adjectives[hash % Adjectives.Length];
        var noun = Nouns[(hash / Adjectives.Length) % Nouns.Length];
        return $"{adj} {noun}";
    }

    public async Task Register(string hardwareUuid, string computerName, string version)
    {
        ConnectedAgents[hardwareUuid] = Context.ConnectionId;
        Context.Items["HardwareUuid"] = hardwareUuid;

        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ItsmDbContext>();

        var agent = await db.Agents.FindAsync(hardwareUuid);
        if (agent is null)
        {
            db.Agents.Add(new AgentRecord
            {
                HardwareUuid = hardwareUuid,
                ComputerName = computerName,
                DisplayName = GenerateDisplayName(hardwareUuid),
                AgentVersion = version,
                IsConnected = true,
                FirstSeenUtc = DateTime.UtcNow,
                LastSeenUtc = DateTime.UtcNow
            });
        }
        else
        {
            agent.ComputerName = computerName;
            agent.AgentVersion = version;
            agent.IsConnected = true;
            agent.LastSeenUtc = DateTime.UtcNow;
        }

        await db.SaveChangesAsync();
        logger.LogInformation("Agent registered: {HardwareUuid} ({ComputerName}) v{Version}", hardwareUuid, computerName, version);
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var hardwareUuid = Context.Items["HardwareUuid"] as string;
        if (hardwareUuid is not null)
        {
            ConnectedAgents.TryRemove(hardwareUuid, out _);

            using var scope = scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ItsmDbContext>();

            var agent = await db.Agents.FindAsync(hardwareUuid);
            if (agent is not null)
            {
                agent.IsConnected = false;
                agent.LastSeenUtc = DateTime.UtcNow;
                await db.SaveChangesAsync();
            }

            logger.LogInformation("Agent disconnected: {HardwareUuid}", hardwareUuid);
        }

        await base.OnDisconnectedAsync(exception);
    }

    public void SendLog(LogEntry entry)
    {
        var hardwareUuid = Context.Items["HardwareUuid"] as string;
        if (hardwareUuid is not null)
        {
            logService.AddLog(hardwareUuid, entry);
        }
    }

    public static string? GetConnectionId(string hardwareUuid) =>
        ConnectedAgents.TryGetValue(hardwareUuid, out var id) ? id : null;
}
