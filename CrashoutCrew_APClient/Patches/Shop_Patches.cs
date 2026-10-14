using Aggro.Core;
using HarmonyLib;

namespace CrashoutCrew_APClient.Patches;

[HarmonyPatch(typeof(Shop))]
public static class Shop_Patches
{
    [HarmonyPatch(nameof(Shop.OnEntityCreated))]
    [HarmonyPrefix]
    public static bool OnEntityCreated(Shop __instance)
    {
        if (!__instance.isServer || GameUtil.isTutorial)
            return true;

        var random = MathUtil.GetRandom(Hash.Calculate(GameUtil.seed, Hash.Calculate(__instance.GetType())));

        var fullRandomDeck = GameManager_Patches.ShopCards;
        __instance._serverRandomDeck = new Deck<ShopItemObject>(random.NextInt());
        
        foreach (var card in fullRandomDeck)
            __instance._serverRandomDeck.AddCard(card.item, card.cardCount);

        __instance._serverDeck = new Deck<Shop.ShopCard>(random.NextInt());

        __instance._serverDeck.AddCard(
            new Shop.ShopCard { type = Shop.ItemType.Random },
            __instance._serverRandomDeck.cardCount
        );

        __instance._serverDeck.Shuffle();

        __instance._serverSeed = random.NextInt();

        return false;
    }
    
    [HarmonyPatch(nameof(Shop.CanAddShopItem))]
    [HarmonyPrefix]
    public static bool CanAddShopItem(ShopItemObject candidate)
    {
        return PluginMain.SaveDataHandler!.APSaveData?.ShopUnlocks.Contains(candidate.itemName) ?? false;
    }
}