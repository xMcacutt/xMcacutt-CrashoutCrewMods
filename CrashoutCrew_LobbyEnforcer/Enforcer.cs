using System;
using System.Collections.Generic;
using System.Linq;
using Mirror;

namespace CrashoutCrew_LobbyEnforcer;

public static class Enforcer
{
    private static readonly Dictionary<string, ModEntry> EnforcedMods = new();
    public static event Action? ConnectionToServerValidated;
    public static event Action<NetworkConnectionToClient>? ServerOnClientValidated;
    
    internal static void NotifyConnectionToServerValidated() 
        => ConnectionToServerValidated?.Invoke();

    internal static void NotifyClientValidatedByServer(NetworkConnectionToClient conn) 
        => ServerOnClientValidated?.Invoke(conn);
    
    public static List<ModEntry> GetRegisteredMods() => [..EnforcedMods.Values];
    
    public static void Enforce(string modId, string modVersion, EnforcementDirection direction)
    {
        EnforcedMods[modId] = new ModEntry
        {
            modId = modId,
            modVersion = modVersion,
            direction = direction
        };
    }
    
    public static void Enforce(string modId, string modVersion, EnforcementDirection direction, string data)
    {
        EnforcedMods[modId] = new ModEntry
        {
            modId = modId,
            modVersion = modVersion,
            direction = direction,
            data = data
        };
    }
    
    public static void UpdateEnforceData(string modId, string data)
    {
        if (EnforcedMods.TryGetValue(modId, out var mod))
            mod.data = data;
    }
    
    internal static bool ValidateRemote(List<ModEntry> remoteMods, bool isRemoteServer, out string failureReason)
    {
        failureReason = string.Empty;
        var remoteModDict = new Dictionary<string, ModEntry>();
        foreach (var mod in remoteMods)
            remoteModDict[mod.modId] = new ModEntry
            {
                modId = mod.modId, 
                modVersion = mod.modVersion, 
                direction = mod.direction,
                data = mod.data,
            };

        var missingMods = new List<string>();
        var modVersionMismatches = new List<(string modId, string localVersion, string remoteVersion)>();
        var modDataMismatches = new List<(string modId, string localData, string remoteData)>();
        foreach (var local in EnforcedMods.Values)
        {
            var remoteMustHave = isRemoteServer 
                ? local.direction is EnforcementDirection.Server or EnforcementDirection.Both
                : local.direction is EnforcementDirection.Clients or EnforcementDirection.Both;
            if (!remoteMustHave) 
                continue;
            if (!remoteModDict.TryGetValue(local.modId, out var remote))
                missingMods.Add(local.modId);
            else
            {
                if (remote.modVersion != local.modVersion)
                    modVersionMismatches.Add((local.modId, local.modVersion, remote.modVersion));
                if (remote.data != local.data)
                    modDataMismatches.Add((local.modId, local.data, remote.data));
            }
        }

        if (missingMods.Count <= 0) 
            return true;
        failureReason = missingMods.Aggregate(failureReason, (current, missingMod)
            => current + $"Missing required mod: {missingMod}\n");
        failureReason = modVersionMismatches.Aggregate(failureReason, (current, mismatchedMod) 
            => current + $"Version mismatch for {mismatchedMod.modId}. Local: {mismatchedMod.localVersion}, Remote: {mismatchedMod.remoteVersion}\n");
        failureReason = modDataMismatches.Aggregate(failureReason, (current, mismatchedMod) 
            => current + $"Data mismatch for {mismatchedMod.modId}. Local: {mismatchedMod.localData}, Remote: {mismatchedMod.remoteData}\n");
        return false;
    }

    internal static bool RequiresRemoteEnforcer(bool isRemoteServer) => EnforcedMods.Values.Any(local 
        => (isRemoteServer && local.direction is EnforcementDirection.Server or EnforcementDirection.Both) 
           || (!isRemoteServer && local.direction is EnforcementDirection.Clients or EnforcementDirection.Both));
}