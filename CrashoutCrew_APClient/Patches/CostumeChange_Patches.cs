using HarmonyLib;

namespace CrashoutCrew_APClient.Patches;

[HarmonyPatch(typeof(CostumeChangeUI))]
public static class CostumeChange_Patches
{
    [HarmonyPatch(nameof(OnEntityCreated))]
    [HarmonyPrefix]
    public static bool OnEntityCreated(CostumeChangeUI __instance)
    {
        __instance.transform.parent.gameObject.SetActive(false);
        return false;
    }
}