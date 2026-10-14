using HarmonyLib;

namespace CrashoutCrew_APClient.Patches;

[HarmonyPatch(typeof(NitroController))]
public class NitroController_Patches
{
    [HarmonyPatch(nameof(NitroController.LocalPlayerGetChargeCount))]
    [HarmonyPrefix]
    public static void OnLocalPlayerGetChargeCount(NitroController __instance)
    {
        __instance.maxNitroCharges = 1;
        if (PluginMain.SaveDataHandler?.APSaveData == null)
            return;
        __instance.maxNitroCharges = PluginMain.SaveDataHandler.APSaveData.BoostUpgrades + 1;
    }
}