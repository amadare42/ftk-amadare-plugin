using System;
using AmadarePlugin.Loadouts;
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

        private void Awake()
        {
            Instance = this;
            OptionsManager.Init(this.Config);

            SteamFixer.Run();
            SkipIntro.Run();
            this.loadoutManager = new LoadoutManager();
            this.loadoutManager.Init();
            this.uiTweaks = new UiTweaks();
            InventoryOnSinglePress();
            
            Logger.LogInfo($"Plugin {PluginInfo.PLUGIN_GUID} is loaded!");
        }

        private static void InventoryOnSinglePress()
        {
            On.CharacterOverworld.CheckInput += (orig, self) =>
            {
                if (!OptionsManager.InventoryOnSinglePress)
                {
                    orig(self);
                    return;
                }
                if (self.m_InputFocus.GetButtonDown("Inventory"))
                    self.StartCoroutine(self.InventoryToggleSequence(true));
                if (!OverworldCamera.Instance.m_Camera.enabled)
                    return;
                self.OverworldCameraControl();
            };
        }
    }
}
