using HarmonyLib;

namespace CrashoutCrew_APClient.Patches;

[HarmonyPatch(typeof(ShiftManager))]
public class ShiftManager_Patches
{
    [HarmonyPatch(nameof(ShiftManager.ServerTruckCompleted))]
    [HarmonyPrefix]
    public static void OnServerTruckCompleted(ShiftManager __instance)
    {
        var truckIndex = __instance.GetTrucksCompleted();
        var shiftIndex = __instance.GetCurrentShift() - 1;
        var contractIndex = GameUtil.contract.bellsRequired / 5;
        APConsole.Instance.DebugLog($"Truck #{truckIndex + 1} | Shift #{shiftIndex + 1} | Contract #{contractIndex + 1}");
        ArchipelagoHandler.Instance.CheckLocation(0x30 * contractIndex + 0x9 * shiftIndex + truckIndex + 2);
    }
    
    [HarmonyPatch(nameof(ShiftManager.TransitionShiftToShiftWon))]
    [HarmonyPrefix]
    public static void OnTransitionShiftToShiftWon(ShiftManager __instance, int seed)
    {
        var shiftIndex = __instance.GetCurrentShift() - 1;
        var contractIndex = GameUtil.contract.bellsRequired / 5;
        APConsole.Instance.DebugLog($"Shift #{shiftIndex + 1} | Contract #{contractIndex + 1}");
        ArchipelagoHandler.Instance.CheckLocation(0x30 * contractIndex + 0x9 * shiftIndex + 1);
    }

    [HarmonyPatch(nameof(ShiftManager.TransitionShiftToGameWon))]
    [HarmonyPrefix]
    public static void OnTransitionShiftToGameWon(int seed)
    {
        var contractIndex = GameUtil.contract.bellsRequired / 5;
        APConsole.Instance.DebugLog($"Shift #{5} | Contract #{contractIndex + 1}");
        ArchipelagoHandler.Instance.CheckLocation(0x30 * contractIndex + 0x9 * 0x4 + 1);
        
        if (contractIndex == ArchipelagoHandler.Instance.slotData?.ContractCount - 1)
            ArchipelagoHandler.Instance.Release();
    }
}