using Aggro.Core;
using HarmonyLib;

namespace CrashoutCrew_APClient.Patches;

[HarmonyPatch(typeof(LocalizedText))]
public static class LocalizedText_Patches
{
    [HarmonyPatch(nameof(LocalizedText.LoadMainTable))]
    [HarmonyPostfix]
    public static void LoadMainTable(bool forceSerialization = true)
    {
        if (PluginMain.LocalizationsPath == null)
            return;
        Localizer.AddCustomLocalizations(PluginMain.LocalizationsPath);
    }
}