using System.Linq;
using AmadarePlugin.Extensions;
using AmadarePlugin.Features.Loadouts.Sync;
using BepInEx.Configuration;
using FTKAPI.Managers;

namespace AmadarePlugin.Options;

public static class OptionsManager
{
    public static bool TestFit = true;
    public static bool HideExtraSlots = true;
    public static bool MaximizeStatButtons = true;
    public static bool SkipIntro = true;
    public static bool InventoryOnSinglePress = true;
    public static bool CustomXpString;
    public static bool HighlightOneTimeEncounters;
    public static bool DisplayPoisonTurns;
    public static bool DebugLogging;
    public static bool ShowBasePrice;
    public static bool AlwaysShare;
    public static bool AddSteamAppId;

    public static BindCollection bindGeneral;
    public static BindCollection bindLoadouts;

    public static void Init(ConfigFile configFile)
    {
        configFile.SaveOnConfigSet = true;
        
        bindGeneral = configFile.BindCollection("General")
            .Bind(
                key: "Skip Intro",
                description: "If enabled, into would be skipped along with \"Prepare to die\" message on game start.",
                set: v => SkipIntro = v,
                defaultValue: true)
            .Bind(
                key: "Single press inventory",
                description:
                "If enabled, inventory would be opened on single \"I\" press instead of just focusing belt.",
                set: v => InventoryOnSinglePress = v,
                defaultValue: true)
            .Bind(
                key: "Better XP string",
                description:
                "If enabled, overrides xp string that it would be more useful: <xp in this level> / <xp to next level> (<xp total>)",
                set: v => CustomXpString = v,
                defaultValue: true)
            .Bind(
                key: "Highlight one time encounters",
                description:
                "If enabled, encounters that will disappear immediately after encounter, would be marked with '!' in title and additional text in description.",
                set: v => HighlightOneTimeEncounters = v,
                defaultValue: true)
            .Bind(
                key: "Display poison turns",
                description:
                "If enabled, when hovering on poison, it would be displayed how many poison ticks left until it wears off.",
                set: v => DisplayPoisonTurns = v,
                defaultValue: true)
            .Bind(
                key: "Display price",
                description:
                "Show base item price in item card. Please note that this price is just an estimate, depending on where you will be selling, price will vary.",
                set: v => ShowBasePrice = v,
                defaultValue: true);


        bindLoadouts = configFile.BindCollection("Loadouts")
            .Bind(
                key: "Test fit",
                description: "If enabled, when hovering on loadout, stat changes would be displayed on character stats",
                set: v => TestFit = v,
                defaultValue: true)
            .Bind(
                key: "Hide Extra Slots",
                description: "If enabled, unoccupied loadout slots will not be displayed",
                set: v => HideExtraSlots = v,
                defaultValue: true)
            .Bind(
                key: "Maximize Stat Buttons",
                description: "If enabled, auto-generated loadouts would be displayed that would maximize specific stat",
                set: v => MaximizeStatButtons = v,
                defaultValue: true)
            .Bind(
                key: "Always Share",
                description:
                "If enabled, each character is considered \"share-enabled\", so no checkbox will be present.",
                set: v =>
                {
                    AlwaysShare = v;
                    if (LoadoutSync.Current.IsOk()) LoadoutSync.Current.Push();
                },
                defaultValue: false);

        configFile.BindCollection("Debug")
            .Bind(
                key: "Enable debug logging",
                description: "If enabled, there would be more logs. Useful for debugging.",
                set: v => DebugLogging = v,
                defaultValue: false)
            .Bind(
                key: "Generate steam_appid",
                description: "If enabled, will generate steam_appid.txt. That will fix some issues on game starting",
                set: v => AddSteamAppId = v,
                defaultValue: true);
        
        foreach (var entry in bindGeneral.Bindings.Concat(bindLoadouts.Bindings))
        {
            PluginConfigManager.RegisterBinding(entry);
        }
    }
}