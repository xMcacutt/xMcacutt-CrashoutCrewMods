
using Aggro.Core;
using HarmonyLib;

namespace CrashoutCrew_APClient.Patches;

[HarmonyPatch(typeof(SettingsUI))]
public class SettingsUI_Patches
{
    [HarmonyPatch(nameof(SettingsUI.OpenSettings))]
    [HarmonyPrefix]
    public static bool OpenSettings(SettingsUI __instance)
    {
        __instance.clickCatch.SetActive(true);
        AggroInputManager.PushController(__instance);
        AggroSettings.ShowSettings("ap", __instance.settingsParent, AggroInputManager.mode);
        return false;
    }
}