using MonoMod.Utils;

namespace AmadarePlugin.Features.Loadouts.MaximizeStats;

public enum StatType
{
    Strength,
    Intelligence,
    Awareness,
    Vitality,
    Speed,
    Talent,
    Luck,
    
    GoldMultiplayer,
    Crit,
    
    COUNT
    
    // Armor,
    // Resistance,
    // Evade
}

public static class StatTypeExtensions
{
    public static string ToFriendlyString(this StatType stat)
    {
        if (stat == StatType.GoldMultiplayer)
        {
            return "Gold Mult.";
        }
        
        return stat.ToString("G").SpacedPascalCase();
    }
}