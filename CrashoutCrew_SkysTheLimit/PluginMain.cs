using BepInEx;
using CrashoutCrew_LobbyEnforcer;
using TemplatePlugin;
using HarmonyLib;
using UnityEngine.SceneManagement;

namespace CrashoutCrew_SkysTheLimit;

[BepInDependency("crashout_crew_lobby_enforcer")]
[BepInPlugin(TemplatePluginInfo.PLUGIN_GUID, TemplatePluginInfo.PLUGIN_NAME, Version)]
public class PluginMain : BaseUnityPlugin
{
    public const string GameName = TemplatePluginInfo.GAME_NAME;
    private const string Version = "1.0.1";

    private readonly Harmony _harmony = new(TemplatePluginInfo.PLUGIN_GUID);

    void Awake()
    {
        Enforcer.Enforce(TemplatePluginInfo.PLUGIN_GUID, Version, EnforcementDirection.Both);
        _harmony.PatchAll();
        DontDestroyOnLoad(gameObject);
        SceneManager.sceneLoaded += (scene, mode) => StackNetworkHandler.Init();
    }
}