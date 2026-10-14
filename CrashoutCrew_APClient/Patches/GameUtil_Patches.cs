using System.Collections;
using Aggro.Core;
using HarmonyLib;

namespace CrashoutCrew_APClient.Patches;

[HarmonyPatch(typeof(GameUtil))]
public class GameUtil_Patches
{
    [HarmonyPatch(nameof(GameUtil.InitializeGameCo))]
    [HarmonyPrefix]
    public static bool Prefix(ref IEnumerator __result)
    {
        __result = InitializeGameCo();
        return false;
    }

    private static IEnumerator InitializeGameCo()
    {
        if (GameUtil._gameInitialized) 
            yield break;
        GameUtil._gameInitialized = true;
        yield return new WaitForTask(Platform.InitializeAsync());
        AggroUtil.InitializeGlobalScrobs();
        AudioManager.Initialize();
        Options.Initialize();
        if (Platform.GetPlatformType() == PlatformType.SteamDeck)
            AggroInputManager.ChangeMode(InputMode.Gamepad);
    }
}