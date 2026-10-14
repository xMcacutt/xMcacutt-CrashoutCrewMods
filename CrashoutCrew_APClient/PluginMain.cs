using System;
using System.Collections.Generic;
using System.IO;
using Aggro.Core;
using BepInEx;
using BepInEx.Logging;
using CrashoutCrew_APClient.Patches;
using CrashoutCrew_LobbyEnforcer;
using CrashoutCrew_SkysTheLimit;
using TemplatePlugin;
using HarmonyLib;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace CrashoutCrew_APClient;

[BepInDependency("crashout_crew_skys_the_limit")]
[BepInDependency("crashout_crew_lobby_enforcer")]
[BepInDependency("crashout_crew_localizer_fallbacks")]
[BepInPlugin(TemplatePluginInfo.PLUGIN_GUID, TemplatePluginInfo.PLUGIN_NAME, Version)]
public class PluginMain : BaseUnityPlugin
{
    public static readonly int CONNECT_BUTTON_ID = AggroSettings.IdToHash("ap-connect");
    public static string? LocalizationsPath;
    
    public static bool EnableDebugLogging;
    public static bool FilterLog;
    public static float MessageInTime
    {
        get => APConsole.Instance.slideInTime;
        set => APConsole.Instance.slideInTime = value;
    }
        
    public static float MessageHoldTime
    {
        get => APConsole.Instance.holdTime;
        set => APConsole.Instance.holdTime = value;
    }
        
    public static float MessageOutTime
    {
        get => APConsole.Instance.fadeOutTime;
        set => APConsole.Instance.fadeOutTime = value;
    }

    public const string GameName = TemplatePluginInfo.GAME_NAME;
    private const string Version = "1.0.0";

    private readonly Harmony _harmony = new(TemplatePluginInfo.PLUGIN_GUID);
    public static ManualLogSource? logger;
    public static SaveDataHandler? SaveDataHandler;
    public static Dictionary<string, AssetBundle> CustomBundles = new();

    void Awake()
    {
        Enforcer.Enforce(TemplatePluginInfo.PLUGIN_GUID, Version, EnforcementDirection.Both);
        CustomBundles = new Dictionary<string, AssetBundle>();
        logger = Logger;
        _harmony.PatchAll();
        DontDestroyOnLoad(gameObject);
        gameObject.AddComponent<ArchipelagoHandler>();
        gameObject.AddComponent<ItemHandler>();
        SaveDataHandler = gameObject.AddComponent<SaveDataHandler>();
        ArchipelagoHandler.OnDisconnect += () =>
        {
            if (SceneManager.GetActiveScene().name != "scene-title")
                SceneManager.LoadScene("scene-title");
            GameManager_Patches.ClearContracts();
            AggroSettings.GetSetting<ButtonSetting>(CONNECT_BUTTON_ID).SetButtonLabel("Connect");
            AggroSettings.RefreshSettingUIs();
        };
        SceneManager.sceneLoaded += (scene, mode) =>
        {
            if (SaveDataHandler?.APSaveData != null)
                SkysTheLimitAPI.SetStackLimit(SaveDataHandler.APSaveData.StrengthUpgrades + 3);
        };
        var modDir = Path.GetDirectoryName(Info.Location);
        if (modDir == null)
            throw new Exception("Mod directory not found");
        LocalizationsPath = Path.Combine(modDir, "Localizations", "locs.json");
        foreach (var bundlePath in Directory.GetFiles(Path.Combine(modDir, "Bundles")))
        {
            var bundle = AssetBundle.LoadFromFile(bundlePath);
            CustomBundles.Add(Path.GetFileNameWithoutExtension(bundlePath), bundle);
        }
        SkysTheLimitAPI.SetStackLimit(3);
    }
}