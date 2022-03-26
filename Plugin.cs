using AmadarePlugin.Common;
using AmadarePlugin.Features;
using AmadarePlugin.Features.Loadouts;
using AmadarePlugin.Features.Loadouts.Sync;
using AmadarePlugin.Options;
using AmadarePlugin.Saving;
using BepInEx;
using BepInEx.Logging;
using FTKAPI.Managers;

namespace AmadarePlugin
{
    [BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    [BepInDependency("FTKAPI")]
    public class Plugin : BaseUnityPlugin
    {
        public static Plugin Instance;
        public static ManualLogSource Log => Instance.Logger;

        public LoadoutManager loadoutManager;
        public UiTweaks uiTweaks;
        public ItemCardPrice itemCardPrice;
        public GameSaveInterceptor saveInterceptor;
        public CharacterShareTracker shareTracker;
        public LoadoutRepository loadoutRepository;

        private void Awake()
        {
            Instance = this;
            OptionsManager.Init(this.Config);
            
            CachedDB.Init();
            SkipIntro.Init();
            SinglePressInventory.Init();
            LoadoutSync.Init();
            SteamFixer.Run();
            
            InitCompositionRoot();
            NetworkManager.RegisterNetObject("AmadareQoL.SyncObject", go => LoadoutSync.Instantiate(go, this.loadoutRepository, this.shareTracker, this.loadoutManager));
            
            Logger.LogInfo($"Plugin {PluginInfo.PLUGIN_GUID} is loaded!");
        }

        private void InitCompositionRoot()
        {
            // loadout & share
            this.saveInterceptor = new GameSaveInterceptor();
            this.shareTracker = new CharacterShareTracker(this.saveInterceptor);
            this.loadoutRepository = new LoadoutRepository(this.saveInterceptor);
            this.loadoutManager = new LoadoutManager(this.shareTracker, this.loadoutRepository);
            
            // other features
            this.uiTweaks = new UiTweaks();
            this.itemCardPrice = new ItemCardPrice();
        }
    }
}
