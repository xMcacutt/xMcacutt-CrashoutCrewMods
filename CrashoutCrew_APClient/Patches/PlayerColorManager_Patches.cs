using Aggro.Core;
using HarmonyLib;

namespace CrashoutCrew_APClient.Patches;

[HarmonyPatch(typeof(PlayerColorManager))]
public class PlayerColorManager_Patches
{
    [HarmonyPatch(nameof(PlayerColorManager.OnEntityCreated))]
    [HarmonyPrefix]
    public static bool OnEntityCreated(PlayerColorManager __instance)
    {
        return SaveManager.isInitialized;
    }
}