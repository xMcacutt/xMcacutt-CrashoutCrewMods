using System.Reflection;
using HarmonyLib;
using Mirror;

namespace CrashoutCrew_LobbyEnforcer.Patches;

[HarmonyPatch]
public static class NetworkClient_Patches
{
    [HarmonyTargetMethod]
    public static MethodBase TargetMethod()
    {
        var sendMethod = typeof(NetworkClient).GetMethod(
            nameof(NetworkClient.Send), 
            BindingFlags.Public | BindingFlags.Static
        );
        return sendMethod?.MakeGenericMethod(typeof(NetMsgServerPlayerJoined))!;
    }
    
    [HarmonyPrefix]
    public static void Prefix(ref NetMsgServerPlayerJoined message)
    {
        var mods = Enforcer.GetRegisteredMods();
        var serializedMods = ModEntry.SerializeList(mods);
        message.playerName = $"{message.playerName ?? string.Empty}\0LE:{serializedMods}";
    }
}