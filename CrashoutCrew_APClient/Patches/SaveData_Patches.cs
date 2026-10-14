using System.Collections.Generic;
using Aggro.Core;
using HarmonyLib;

namespace CrashoutCrew_APClient.Patches;

[HarmonyPatch(typeof(SaveData))]
public class SaveData_Patches
{
    [HarmonyPatch(nameof(SaveData.GetTotalBells))]
    [HarmonyPrefix]
    public static bool GetTotalBells(SaveData __instance, out int __result)
    {
        __result = PluginMain.SaveDataHandler!.APSaveData?.Bells ?? 0;
        return false;
    }
    
    [HarmonyPatch(nameof(SaveData.GetUnlockedCostumes), [])]
    [HarmonyPrefix]
    public static bool GetUnlockedCostumes(ref CostumeObject[] __result)
    {
        __result = new List<CostumeObject>(GlobalScriptableObject<CosmeticGlobalData>.instance.costumes).ToArray();
        return false;
    }
}