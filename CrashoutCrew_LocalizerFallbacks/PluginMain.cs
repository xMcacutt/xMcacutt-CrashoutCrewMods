using BepInEx;
using TemplatePlugin;
using HarmonyLib;

namespace CrashoutCrew_LocalizerFallbacks
{
    [BepInPlugin(TemplatePluginInfo.PLUGIN_GUID, TemplatePluginInfo.PLUGIN_NAME, Version)]
    public class PluginMain : BaseUnityPlugin
    {
        public const string GameName = TemplatePluginInfo.GAME_NAME;
        private const string Version = "1.0.0";

        private readonly Harmony _harmony = new Harmony(TemplatePluginInfo.PLUGIN_GUID);

        void Awake()
        {
            _harmony.PatchAll();
        }
    }
}