using System;
using System.Linq;
using Aggro.Core;
using HarmonyLib;
using UnityEngine;

namespace CrashoutCrew_APClient.Patches;

[HarmonyPatch(typeof(AggroSettingsObject))]
public static class AggroSettingsObject_Patches
{
    [HarmonyPatch(nameof(AggroSettingsObject.TryGetTemplate))]
    [HarmonyPostfix]
    public static void TryGetTemplate(Type type, ref GameObject template, ref bool __result)
    {
        if (typeof(TextSetting).IsAssignableFrom(type) || type.Name == "TextSetting")
        {
            if (PluginMain.CustomBundles.TryGetValue("aggro-text-setting", out var bundle))
            {
                template = bundle.LoadAsset<GameObject>("prefab-aggrosetting-text");
                __result = template != null;
                return;
            }
        }
        
        if (typeof(ButtonSetting).IsAssignableFrom(type) || type.Name == "ButtonSetting")
        {
            if (PluginMain.CustomBundles.TryGetValue("aggro-button-setting", out var bundle))
            {
                template = bundle.LoadAsset<GameObject>("prefab-aggrosetting-button");
                __result = template != null;
            }
        }
    }
    
    [HarmonyPatch(nameof(AggroSettingsObject.TryGetCategoryLabel))]
    [HarmonyPrefix]
    public static bool TryGetCategoryLabel(AggroSettingsObject __instance, ref bool __result, string category, out string label)
    {
        __instance.CheckInitialize();
        var categoryExists = __instance._categoryToLabel.TryGetValue(category, out label);
        if (!categoryExists)
        {
            var copy = __instance.categories.ToList();
            copy.Add(new AggroSettingsObject.Category());
            copy.Last().category = category;
            label = category[..1].ToUpper() + category[1..];
            copy.Last().label = label;
            __instance.categories = copy.ToArray();
        }
        __result = true;
        return false;
    } 
}