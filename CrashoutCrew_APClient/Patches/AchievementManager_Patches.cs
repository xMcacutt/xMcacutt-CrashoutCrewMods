using Aggro.Core;
using HarmonyLib;

namespace CrashoutCrew_APClient.Patches;

[HarmonyPatch(typeof(SteamPlatform))]
public class AchievementManager_Patches
{
    [HarmonyPatch(nameof(SteamPlatform.UnlockAchievement), typeof(string))]
    [HarmonyPrefix]
    public static bool UnlockAchievement(string id)
    {
        return false;
    }
}