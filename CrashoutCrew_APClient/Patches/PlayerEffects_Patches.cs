using System.Collections.Generic;
using HarmonyLib;

namespace CrashoutCrew_APClient.Patches;

[HarmonyPatch(typeof(PlayerEffects))]
public class PlayerEffects_Patches
{
    private static readonly Dictionary<int, int> SpeedUpgradePercentage = new()
    {
        { 0, -10 },
        { 1, 0 },
        { 2, 10 },
        { 3, 20 },
    };
    
    [HarmonyPatch(nameof(PlayerEffects.OnUpdateSimulationEarly))]
    [HarmonyPostfix]
    public static void OnUpdateSimulationEarly(PlayerEffects __instance)
    {
        if (PluginMain.SaveDataHandler?.APSaveData == null)
            return;
        __instance.AddVehicleSpeedPercentageRaw(SpeedUpgradePercentage[PluginMain.SaveDataHandler.APSaveData.SpeedUpgrades]);
        __instance.AddVehicleSpeedPercentageMinMax(SpeedUpgradePercentage[PluginMain.SaveDataHandler.APSaveData.SpeedUpgrades]);
    }
}