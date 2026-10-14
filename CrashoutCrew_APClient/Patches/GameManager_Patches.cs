using System.Linq;
using Aggro.Core;
using HarmonyLib;
using UnityEngine;
using Random = System.Random;

namespace CrashoutCrew_APClient.Patches;

[HarmonyPatch(typeof(GameManager))]
public static class GameManager_Patches
{
    private static readonly string[] Excluded = ["order-bingbong"];
    public static DeckCard<ShopItemObject>[] ShopCards = null!;
    private static ContractObject[]? _cachedContracts;

    public static void ClearContracts() => _cachedContracts = null;
    
    [HarmonyPatch(nameof(GameManager.GetContracts))]
    [HarmonyPrefix]
    public static bool GetContracts(GameManager __instance, ref ContractObject[] __result)
    {
        if (GameUtil.isDemo)
            return true;

        if (_cachedContracts != null)
        {
            __result = _cachedContracts;
            return false;
        }

        __result = [];
        var random4 = __instance.contracts.Last();
        ShopCards = random4.shopCards.ToArray();
        if (ArchipelagoHandler.Instance.Seed == null)
        {
            __result = __instance.contracts;
            return false;
        }
        var random = new Random(int.Parse(ArchipelagoHandler.Instance.Seed[..9]));
        var allOrders = Resources.FindObjectsOfTypeAll<ShiftOrderObject>().Where(x => !Excluded.Contains(x.name)).ToArray();
        var contracts2 = ArchipelagoHandler.Instance.slotData?.ContractCountTwo ?? 5;
        var contracts3 = ArchipelagoHandler.Instance.slotData?.ContractCountThree ?? 5;
        var contracts4 = ArchipelagoHandler.Instance.slotData?.ContractCountFour ?? 5;
        _cachedContracts = ContractGenerator.GenerateContracts(random, allOrders, contracts2, contracts3, contracts4);
        __result = _cachedContracts;
        return false;
    }
}