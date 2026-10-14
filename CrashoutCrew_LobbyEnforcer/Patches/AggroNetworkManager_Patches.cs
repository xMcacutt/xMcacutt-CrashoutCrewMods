using System;
using System.Collections;
using System.Threading.Tasks;
using Aggro.Core.Networking;
using HarmonyLib;
using Mirror;
using UnityEngine;

namespace CrashoutCrew_LobbyEnforcer.Patches;

[HarmonyPatch(typeof(AggroNetworkManager))]
public class AggroNetworkManager_Patches
{
    private const string PayloadPrefix = "\0LE:";
    private const string HostHandshakeMarker = "LE_HOST_HANDSHAKE";
    private static bool _serverHandshakeReceived;
    private static bool _serverHandshakeValid;
    
    [HarmonyPatch(nameof(AggroNetworkManager.OnStartClient))]
    [HarmonyPostfix]
    public static void OnStartClient_Postfix()
    {
        _serverHandshakeReceived = false;
        _serverHandshakeValid = false;
    }
    
    [HarmonyPatch(nameof(AggroNetworkManager.OnServerPlayerJoined))]
    [HarmonyPrefix]
    public static bool OnServerPlayerJoinedPrefix(AggroNetworkManager __instance, NetworkConnectionToClient conn, NetMsgServerPlayerJoined msg)
    {
        var rawName = msg.playerName ?? string.Empty;
        var cleanName = rawName;
        var isModdedClient = false;
        var index = rawName.IndexOf(PayloadPrefix, StringComparison.Ordinal);
        if (index >= 0)
        {
            cleanName = rawName[..index];
            var serializedMods = rawName[(index + PayloadPrefix.Length)..];
            isModdedClient = true;
            
            try
            {
                var remoteMods = ModEntry.DeserializeList(serializedMods);
                if (!Enforcer.ValidateRemote(remoteMods, isRemoteServer: false, out var reason))
                {
                    PluginMain.logger!.LogWarning($"[LobbyEnforcer] Rejecting client {conn.address}:\n{reason}");
                    conn.Disconnect();
                    return false;
                }
            }
            catch (Exception ex)
            {
                PluginMain.logger!.LogError($"[LobbyEnforcer] Failed to parse client payload:\n{ex.Message}");
                conn.Disconnect();
                return false;
            }
        }
        if (!isModdedClient)
        {
            if (Enforcer.RequiresRemoteEnforcer(isRemoteServer: false))
            {
                PluginMain.logger!.LogWarning($"[LobbyEnforcer] Rejecting VANILLA client {conn.address}: Server requires client mods.");
                conn.Disconnect();
                return false;
            }
        }
        msg.playerName = cleanName;
        return true;
    }
    
    [HarmonyPatch(nameof(AggroNetworkManager.HandleClientConnectCo))]
    [HarmonyPrefix]
    public static bool HandleClientConnectCoPrefix(AggroNetworkManager __instance, NetworkConnectionToClient conn, ref IEnumerator __result)
    {
        __result = HandleClientConnectWithEnforcement(__instance, conn);
        return false;
    }
    
    private static IEnumerator HandleClientConnectWithEnforcement(AggroNetworkManager manager, NetworkConnectionToClient conn)
    {
        if (conn is not LocalConnectionToClient)
        {
            conn.Send(new NetMsgGameSettings { versionGuid = new Guid(Application.buildGUID) });
            var mods = Enforcer.GetRegisteredMods();
            var serializedMods = ModEntry.SerializeList(mods);
            conn.Send(new NetMsgPlayerJoined { playerName = $"{HostHandshakeMarker}{PayloadPrefix}{serializedMods}" });
        }

        while (!conn.isReady)
            yield return null;

        AggroNetworkManager._behaviourQuery.Run();
        for (var i = 0; i < AggroNetworkManager._behaviourQuery.count; ++i)
            AggroNetworkManager._behaviourQuery[i].ServerClientConnected(conn);

        var slot = -1;
        for (var i = 0; i < AggroNetworkManager._orderedConnections.Count; ++i)
        {
            if (AggroNetworkManager._orderedConnections[i] != null) 
                continue;
            slot = i;
            break;
        }

        if (slot < 0)
        {
            slot = AggroNetworkManager._orderedConnections.Count;
            AggroNetworkManager._orderedConnections.Add(null);
        }

        AggroNetworkManager._orderedConnections[slot] = conn;
        NetworkAggroManagerBase<LobbyManager>.instance.ServerAddPlayer(conn, slot);
        NetworkAggroManagerBase<NetworkPlayerManager>.instance.ServerPlayerJoined(conn);
        Enforcer.NotifyClientValidatedByServer(conn);
    }
    
    [HarmonyPatch(nameof(AggroNetworkManager.OnPlayerJoined))]
    [HarmonyPrefix]
    public static bool OnPlayerJoinedPrefix(NetMsgPlayerJoined msg)
    {
        if (msg.playerName == null || !msg.playerName.StartsWith(HostHandshakeMarker)) 
            return true;
        var index = msg.playerName.IndexOf(PayloadPrefix, StringComparison.Ordinal);
        if (index < 0) 
            return false;
        var serializedMods = msg.playerName[(index + PayloadPrefix.Length)..];
        _serverHandshakeReceived = true;
        try
        {
            var remoteMods = ModEntry.DeserializeList(serializedMods);
            _serverHandshakeValid = Enforcer.ValidateRemote(remoteMods, isRemoteServer: true, out var reason);
            if (!_serverHandshakeValid)
            {
                PluginMain.logger!.LogError($"[LobbyEnforcer] Host failed mod validation:\n{reason}");
                PlayerMessageManager.QueueMessage($"Host failed mod validation:\n{reason}", true, true);
            }
        }
        catch (Exception ex)
        {
            PluginMain.logger!.LogError($"[LobbyEnforcer] Failed to parse host payload:\n{ex.Message}");
            PlayerMessageManager.QueueMessage($"Failed to parse host payload:\n{ex.Message}", true, true);
            _serverHandshakeValid = false;
        }
        return false;
    }
    
    [HarmonyPatch(nameof(AggroNetworkManager.StartClientAsync))]
    [HarmonyPostfix]
    public static async Task<ClientConnectionResult> StartClientAsyncPostfix(Task<ClientConnectionResult> originalTask)
    {
        var result = await originalTask;
        if (!result.isSuccess) return result;
        var timeout = 1.0f;
        while (!_serverHandshakeReceived && timeout > 0)
        {
            timeout -= Time.deltaTime;
            await Task.Yield();
        }
        if (_serverHandshakeReceived)
        {
            if (_serverHandshakeValid)
            {
                Enforcer.NotifyConnectionToServerValidated();
                return result;
            }
            AggroNetworkManager.Disconnect();
            return ClientConnectionResult.Failed();
        }
        if (!Enforcer.RequiresRemoteEnforcer(isRemoteServer: true))
            return result;
        PluginMain.logger!.LogError("[LobbyEnforcer] Server is missing required mods or LobbyEnforcer.");
        PlayerMessageManager.QueueMessage("Server is missing required mods or LobbyEnforcer.", true, true);
        AggroNetworkManager.Disconnect();
        return ClientConnectionResult.Failed();
    }
}