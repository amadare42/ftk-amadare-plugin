using BepInEx.Configuration;

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
    public static bool DebugLogging = false;

    public static void Init(ConfigFile configFile)
    {
        SkipIntro = configFile.Bind("General", "Skip Into", true,
                "If enabled, into would be skipped along with \"Prepare to die\" message on game start.")
            .Value;
        InventoryOnSinglePress = configFile.Bind("General", "Single press inventory", true,
                "If enabled, inventory would be opened on single \"I\" press instead of just focusing belt.")
            .Value;
        CustomXpString = configFile.Bind("General", "Better XP string", true,
                "If enabled, overrides xp string that it would be more useful: <xp in this level> / <xp to next level> (<xp total>)")
            .Value;
        HighlightOneTimeEncounters = configFile.Bind("General", "Highlight one time encounters", true,
                "If enabled, encounters that will disappear immediately after encounter, would be marked with '!' in title and additional text in description.")
            .Value;
        DisplayPoisonTurns = configFile.Bind("General", "Display poison turns", true,
                "If enabled, when hovering on poison, it would be displayed how many poison ticks left until it wears off.")
            .Value;
        
        TestFit = configFile.Bind("Loadouts", "Test fit", true,
                "If enabled, when hovering on loadout, stat changes would be displayed on character stats")
            .Value;
        HideExtraSlots = configFile.Bind("Loadouts", "Hide Extra Slots", true,
                "If enabled, unoccupied loadout slots will not be displayed")
            .Value;
        MaximizeStatButtons = configFile.Bind("Loadouts", "Maximize Stat Buttons", true,
                "If enabled, auto-generated loadouts would be displayed that would maximize specific stat")
            .Value;
        
        DebugLogging = configFile.Bind("Debug", "Enable debug logging", false,
                "If enabled, there would be more logs. Useful for debugging.")
            .Value;
    }
}