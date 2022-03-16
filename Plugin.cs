using AmadarePlugin.Common;
using AmadarePlugin.Features;
using AmadarePlugin.Features.Loadouts;
using AmadarePlugin.Options;
using BepInEx;
using BepInEx.Logging;

namespace AmadarePlugin
{
    [BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    public class Plugin : BaseUnityPlugin
    {
        public static Plugin Instance;
        public static ManualLogSource Log => Instance.Logger;

        private LoadoutManager loadoutManager;
        private UiTweaks uiTweaks;
        private ItemCardPrice itemCardPrice;
        private PluginOptionsUi optionsUi;

        private void Awake()
        {
            Instance = this;
            this.optionsUi = new PluginOptionsUi();
            OptionsManager.Init(this.Config, this.optionsUi);
            this.optionsUi.Init();
            CachedDB.Init();

            SteamFixer.Run();
            SkipIntro.Run();
            this.loadoutManager = new LoadoutManager();
            this.loadoutManager.Init();
            this.uiTweaks = new UiTweaks();
            SinglePressInventory.Init();
            this.itemCardPrice = new ItemCardPrice();
            
            Logger.LogInfo($"Plugin {PluginInfo.PLUGIN_GUID} is loaded!");
        }
    }
}
