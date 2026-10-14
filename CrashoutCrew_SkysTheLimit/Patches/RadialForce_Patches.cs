using HarmonyLib;

namespace CrashoutCrew_SkysTheLimit.Patches;

[HarmonyPatch(typeof(PlayerLiftVisual))]
public static class PlayerLiftVisual_Patches
{
    [HarmonyPatch(nameof(PlayerLiftVisual.OnEntityCreated))]
    [HarmonyPrefix]
    public static void OnEntityCreated(PlayerLiftVisual __instance)
    {
        __instance.stackPower = 1.03f;
        __instance.velRotOffset = 0.9f;
    }
}