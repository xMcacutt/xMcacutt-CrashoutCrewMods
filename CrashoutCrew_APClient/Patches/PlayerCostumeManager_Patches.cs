using Aggro.Core;
using HarmonyLib;

namespace CrashoutCrew_APClient.Patches;

[HarmonyPatch(typeof(PlayerCostumeManager))]
public class PlayerCostumeManager_Patches
{
    [HarmonyPatch(nameof(PlayerCostumeManager.OnEntityCreated))]
    [HarmonyPrefix]
    public static bool OnEntityCreated(PlayerCostumeManager __instance)
    {
        return SaveManager.isInitialized;
    }
}