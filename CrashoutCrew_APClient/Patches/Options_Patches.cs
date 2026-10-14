using Aggro.Core;
using HarmonyLib;

namespace CrashoutCrew_APClient.Patches;

[HarmonyPatch(typeof(Options))]
public static class Options_Patches
{
    private static readonly int SERVER_SETTING_ID = AggroSettings.IdToHash("ap-server");
    private static readonly int SLOT_SETTING_ID = AggroSettings.IdToHash("ap-slot");
    private static readonly int PASS_SETTING_ID = AggroSettings.IdToHash("ap-pass");
    
    [HarmonyPatch(nameof(Options.AddGameSettings))]
    [HarmonyPrefix]
    public static void AddGameSettings()
    {
        AggroSettings.AddSetting("ap-server", "ap", "AP_Server", new TextSetting("archipelago.gg:38281", "", false));
        AggroSettings.AddSetting("ap-slot", "ap", "AP_Slot", new TextSetting("Player1", "", false));
        AggroSettings.AddSetting("ap-pass", "ap", "AP_Password", new TextSetting("", "", true));
        AggroSettings.AddSetting("ap-connect", "ap", "", new ButtonSetting("AP_Connect", () =>
        {
            if (ArchipelagoHandler.Instance.IsConnected)
                ArchipelagoHandler.Instance.Disconnect();
            else
            {
                var server = AggroSettings.GetSetting<TextSetting>(SERVER_SETTING_ID);
                var slot = AggroSettings.GetSetting<TextSetting>(SLOT_SETTING_ID);
                var password = AggroSettings.GetSetting<TextSetting>(PASS_SETTING_ID);

                if (ArchipelagoHandler.Instance == null || !ArchipelagoHandler.Instance.connectionFinished)
                    return;
                
                APConsole.Instance.Log("Connecting...");
                
                if (string.IsNullOrWhiteSpace(server.text))
                {
                    APConsole.Instance.Log("<color=\"red\">[ERROR] Server cannot be empty!</color>");
                    return;
                }
                
                if (string.IsNullOrWhiteSpace(slot.text))
                {
                    APConsole.Instance.Log("<color=\"red\">[ERROR] Slot cannot be empty!</color>");
                    return;
                }
                
                if (!ArchipelagoHandler.Instance.CreateSession(server.text, slot.text, password.text))
                {
                    APConsole.Instance.Log("<color=\"red\">[ERROR] The server cannot be reached or your slot/password is incorrect.</color>");
                    return;
                }
            
                ArchipelagoHandler.Instance.OnConnected += Title_Patches.HandleConnected;
                ArchipelagoHandler.Instance.OnConnectionFailed += Title_Patches.HandleConnectionFailed;

                ArchipelagoHandler.Instance.Connect();
            }
        }));
        AggroSettings.AddSetting("ap-debug", "ap", "AP_Debug_Logging", new ToggleSetting(false, b => { PluginMain.EnableDebugLogging = b; }));
        AggroSettings.AddSetting("ap-filter", "ap", "AP_Filter_Log_To_Self", new ToggleSetting(false,  b => { PluginMain.FilterLog = b; }));
        AggroSettings.AddSetting("ap-message-in", "ap", "AP_Message_In_Time", new FloatSetting(FloatSetting.Style.Number, 0.15f, 0.5f, 0.25f, f => { PluginMain.MessageInTime = f; }));
        AggroSettings.AddSetting("ap-message-hold", "ap", "AP_Message_Hold_Time", new FloatSetting(FloatSetting.Style.Number, 0.5f, 5f, 2.5f, f => { PluginMain.MessageHoldTime = f; }));
        AggroSettings.AddSetting("ap-message-out", "ap", "AP_Message_Out_Time", new FloatSetting(FloatSetting.Style.Number, 0.15f, 0.5f, 0.4f, f => { PluginMain.MessageOutTime = f; }));
        AggroSettings.AddSetting("ap-test-message", "ap", "", new ButtonSetting("AP_Test_Message", () => { APConsole.Instance.Log("This is a test!"); }));
    }
}

    
