using HarmonyLib;

namespace CrashoutCrew_SkysTheLimit.Patches;

[HarmonyPatch(typeof(GrabbableHolder))]
public class GrabbableHolder_Patches
{
    [HarmonyPatch(nameof(GrabbableHolder.OnEntityCreated))]
    [HarmonyPrefix]
    public static void OnEntityCreated(GrabbableHolder __instance)
    {
        __instance.maximumStackHeight = SkysTheLimitAPI.StackLimit;
    }
}