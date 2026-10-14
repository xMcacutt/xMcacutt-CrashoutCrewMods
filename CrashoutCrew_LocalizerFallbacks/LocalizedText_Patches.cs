using System;
using Aggro.Core;
using HarmonyLib;
using UnityEngine;

namespace CrashoutCrew_LocalizerFallbacks;

[HarmonyPatch(typeof(LocalizedText))]
public class LocalizedText_Patches
{
    [HarmonyPatch(nameof(LocalizedText.GetText), typeof(string), typeof(bool))]
    [HarmonyPrefix]
    public static bool GetText(ref string __result, string id, bool printDebug = true)
    {
        var upperId = id.ToUpperInvariant();
        try
        {
            if (LocalizedText.mainTable.TryGetValue(upperId, out var stringList))
            {
                var text = LocalizedText.FailsafeParsing(stringList[(int) LocalizedText.CURRENT_LANGUAGE]);
                if (string.IsNullOrEmpty(text))
                    text = LocalizedText.ReplaceCustomValues(LocalizedText.FailsafeParsing(stringList[0]));
                __result = text;
                return false;
            }
            Debug.LogWarning("Failed to load text: " + upperId + " - Falling back to id!");
            __result = id;
            return false;
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"Failed to load text: {upperId} - {ex} - Falling back to id!");
            __result = id;
            return false;
        }
    }
}