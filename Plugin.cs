using AmadarePlugin.Loadouts;
using BepInEx;
using BepInEx.Logging;

namespace AmadarePlugin
{
    [BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    public class Plugin : BaseUnityPlugin
    {
        public static Plugin Instance;
        public static ManualLogSource Log => Plugin.Instance.Logger;

        private LoadoutManager loadoutManager;
        private SkipIntro skipIntro;
        private UiTweaks improvedDisplay;

        private void Awake()
        {
            Instance = this;
            Logger.LogInfo($"Plugin {PluginInfo.PLUGIN_GUID} is loaded!");

            this.loadoutManager = new LoadoutManager();
            this.loadoutManager.Init();
            this.skipIntro = new SkipIntro();
            this.improvedDisplay = new UiTweaks();
            
            Logger.LogInfo($"Patched!");
        }
    }
}
