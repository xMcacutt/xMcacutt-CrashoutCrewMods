using System;
using System.Collections.Generic;
using System.Reflection.Emit;
using Aggro.Core;
using HarmonyLib;
using TMPro;

namespace CrashoutCrew_APClient.Patches;

[HarmonyPatch(typeof(ContractUI))]
public static class ContractUI_Patches
{
    [HarmonyPatch(nameof(ContractUI.SetUp))]
    [HarmonyPostfix]
    public static void SetUp(ContractUI __instance, int contractNum)
    {
        var checkedLocations = ArchipelagoHandler.Instance.CountLocationsCheckedInRange(0x30 * (contractNum - 1) + 1, 0x30 * contractNum + 1);
        __instance.contractNumText.text = $"{checkedLocations}/{35} locations - {contractNum}";
    }
    
    [HarmonyPatch(nameof(ContractUI.SetUp))]
    public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        var codes = new List<CodeInstruction>(instructions);
        var setIndex = AccessTools.Method(typeof(LocalizedText), nameof(LocalizedText.SetIndex), [typeof(string)]);
        var tmpField = AccessTools.Field(typeof(LocalizedText), nameof(LocalizedText.tmp));
        var setText = AccessTools.Method(typeof(TMP_Text), "set_text");
        for (var i = 0; i < codes.Count; i++)
        {
            if (!codes[i].Calls(setIndex))
                continue;
            if (i < 3 || codes[i - 1].opcode != OpCodes.Ldarg_1)
                throw new InvalidOperationException("ContractUI.SetUp: unexpected SetIndex IL pattern.");
            codes[i - 1] = new CodeInstruction(OpCodes.Ldfld, tmpField);
            codes.Insert(i, new CodeInstruction(OpCodes.Ldarg_1));
            codes[i + 1] = new CodeInstruction(OpCodes.Callvirt, setText);
            break;
        }
        return codes;
    }
}