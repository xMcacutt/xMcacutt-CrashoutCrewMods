using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using UnityEngine;

namespace CrashoutCrew_APClient.Patches;

[HarmonyPatch(typeof(NitroUI))]
public class NitroUI_Patches
{
    [HarmonyPatch(nameof(NitroUI.OnUpdatePresentation))]
    [HarmonyPrefix]
    public static void OnUpdatePresentation(NitroUI __instance)
    {
        if (__instance.nitroBars.Length == 5)
            return;
        var finalBar = __instance.nitroBars.Last();
        var finalBarObj = finalBar.gameObject;
        var barParent = finalBarObj.transform.parent;
        var newBarObj = Object.Instantiate(finalBarObj, barParent);
        var newBar = newBarObj.GetComponent<NitroBarUI>();
        var nitroBars = new List<NitroBarUI>(__instance.nitroBars);
        nitroBars.Add(newBar);
        __instance.nitroBars = nitroBars.ToArray();
    }
}