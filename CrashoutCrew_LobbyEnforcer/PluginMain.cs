using BepInEx;
using BepInEx.Logging;
using TemplatePlugin;
using HarmonyLib;
using UnityEngine;

namespace CrashoutCrew_LobbyEnforcer;

[BepInPlugin(TemplatePluginInfo.PLUGIN_GUID, TemplatePluginInfo.PLUGIN_NAME, Version)]
public class PluginMain : BaseUnityPlugin
{
    public const string GameName = TemplatePluginInfo.GAME_NAME;
    private const string Version = "1.0.2";

    private readonly Harmony _harmony = new(TemplatePluginInfo.PLUGIN_GUID);
    public static ManualLogSource? logger;

    void Awake()
    {
        logger = Logger;
        _harmony.PatchAll();
        DontDestroyOnLoad(gameObject);
    }
}