using System.Linq;
using Aggro.Core;
using CrashoutCrew_LobbyEnforcer;
using HarmonyLib;
using TemplatePlugin;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CrashoutCrew_APClient.Patches;

[HarmonyPatch(typeof(Title))]
public class Title_Patches
{
    private static Button? _connectButton;
    private static Title? _instance;
    private static TextMeshProUGUI? connectTmp;

    [HarmonyPatch(nameof(Title.Start))]
    [HarmonyPrefix]
    public static void Start(Title __instance)
    {
        _instance = __instance;
        _connectButton = __instance.releaseButtons[0].GetComponent<Button>();
        var joinButton = __instance.releaseButtons[2].GetComponent<Button>();
        joinButton.interactable = false;
        var settingsUI = _connectButton.transform.parent.parent.GetComponentInChildren<SettingsUI>();
        var hostButton = __instance.releaseButtons[1].gameObject;
        __instance.releaseButtons = __instance.releaseButtons.Where(x => x.name != "HostGame").ToArray();
        hostButton.SetActive(false);
        Object.Destroy(_connectButton.GetComponentInChildren<LocalizedText>(true));
        connectTmp = _connectButton.GetComponentInChildren<TextMeshProUGUI>(true);
        _connectButton.onClick = new Button.ButtonClickedEvent();
        _connectButton.interactable = ArchipelagoHandler.Instance.IsConnected;
        _connectButton.onClick.AddListener(() =>
        {
            if (ArchipelagoHandler.Instance.IsConnected && SaveManager.isInitialized)
            {
                OnGameHost(_instance!);
                return;
            }
            settingsUI.OpenSettings();
        });
        ArchipelagoHandler.OnDisconnect += HandleDisconnect;
    }

    [HarmonyPatch(nameof(Title.Update))]
    [HarmonyPrefix]
    public static void Update()
    {
        if (connectTmp == null) 
            return;
        connectTmp.text = ArchipelagoHandler.Instance.IsConnected ? "CONTINUE" : "CONNECT";
    }

    private static void OnGameHost(Title __instance)
    {
        PauseManager.Reset();
        if (Platform._result != Platform.InitializeResult.Success)
        {
            APConsole.Instance.Log("Entering Singleplayer!");
            __instance.OnSinglePlayer();
        }
        else
        {
            __instance.StopAllCoroutines();
            __instance.StartCoroutine(__instance.HostCo(true));
        }
    }
    
    public static void HandleConnected(string seed, string slot)
    {
        APConsole.Instance.DebugLog("Connection succeeded!");
        AggroSettings.GetSetting<ButtonSetting>(PluginMain.CONNECT_BUTTON_ID).SetButtonLabel("Disconnect");
        AggroSettings.RefreshSettingUIs();
        Enforcer.UpdateEnforceData(TemplatePluginInfo.PLUGIN_GUID, $"{seed}{slot}");
        PluginMain.SaveDataHandler!.onInitialized += HandleSaveInit;
        PluginMain.SaveDataHandler.InitializeSave();
        ArchipelagoHandler.Instance.OnConnected -= HandleConnected;
        ArchipelagoHandler.Instance.OnConnectionFailed -= HandleConnectionFailed;
    }

    private static void HandleSaveInit()
    {
#pragma warning disable CS8601 // Possible null reference assignment.
        PluginMain.SaveDataHandler!.onInitialized -= HandleSaveInit;
#pragma warning restore CS8601 // Possible null reference assignment.
        _connectButton!.interactable = true;
    }
        
    public static void HandleConnectionFailed(string reason)
    {
        APConsole.Instance.Log("<color=\"red\">[ERROR] The server cannot be reached or your slot/password is incorrect.</color>");
        _connectButton!.interactable = false;
        ArchipelagoHandler.Instance.OnConnected -= HandleConnected;
        ArchipelagoHandler.Instance.OnConnectionFailed -= HandleConnectionFailed;
    }

    public static void HandleDisconnect()
    {
        if (_connectButton != null)
            _connectButton.interactable = false;
    }
}