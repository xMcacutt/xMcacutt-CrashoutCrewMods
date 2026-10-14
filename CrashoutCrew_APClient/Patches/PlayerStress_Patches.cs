using Aggro.Core;
using HarmonyLib;

namespace CrashoutCrew_APClient.Patches;

[HarmonyPatch(typeof(PlayerStress))]
public class PlayerStress_Patches
{
    [HarmonyPatch(nameof(PlayerStress.LocalPlayerGetStressBarAmount))]
    public static bool OnLocalPlayerGetStressBarAmount(PlayerStress __instance, ref float __result)
    {
        var playerUpgrades = ((IEntityBehaviourBase)__instance).entity.GetObject<PlayerUpgrades>();
        __result = 1f;
        if (playerUpgrades.HasUpgrade(PlayerUpgrade.StressAmountUp))
            __result = playerUpgrades.stressBarUpAmount;
        __result -= 0.25f;
        if (PluginMain.SaveDataHandler?.APSaveData != null)
            __result += 0.25f * PluginMain.SaveDataHandler.APSaveData.StressUpgrades;
        return false;
    }
}