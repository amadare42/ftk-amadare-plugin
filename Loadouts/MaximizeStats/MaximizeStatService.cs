using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using AmadarePlugin.Extensions;
using AmadarePlugin.Loadouts.Fitting;
using GridEditor;
using Newtonsoft.Json;
using LoadoutDict = System.Collections.Generic.Dictionary<PlayerInventory.ContainerID, GridEditor.FTK_itembase.ID>;

namespace AmadarePlugin.Loadouts;

public class MaximizeStatService
{
    public static Dictionary<StatType, string> StatToIconMap = new()
    {
        [StatType.Strength] = "iconStrength",
        [StatType.Intelligence] = "iconIntelligence",
        [StatType.Awareness] = "iconAwareness",
        [StatType.Vitality] = "iconVitality",
        [StatType.Speed] = "iconSpeed",
        [StatType.Talent] = "iconTalent",
        [StatType.Luck] = "iconLuck",

        // [StatType.Armor] = mods => mods.m_ModDefensePhysical + mods.m_PartyCombatArmor,
        // [StatType.Resistance] = mods => mods.m_ModDefenseMagic + mods.m_PartyCombatResist,
        // [StatType.Evade] = mods => mods.m_ModEvadeRating + mods.m_PartyCombatEvade,
    };

    private Dictionary<StatType, Func<FTK_characterModifier, float>> accessorsMap = new()
    {
        [StatType.Strength] = mods => mods?.m_ModToughness ?? 0,
        [StatType.Intelligence] = mods => mods?.m_ModFortitude ?? 0,
        [StatType.Awareness] = mods => mods?.m_ModAwareness ?? 0,
        [StatType.Vitality] = mods => mods?.m_ModVitality ?? 0,
        [StatType.Speed] = mods => mods?.m_ModQuickness ?? 0,
        [StatType.Talent] = mods => mods?.m_ModTalent ?? 0,
        [StatType.Luck] = mods => mods?.m_ModLuck ?? 0,
        
        // [StatType.Armor] = mods => mods.m_ModDefensePhysical + mods.m_PartyCombatArmor,
        // [StatType.Resistance] = mods => mods.m_ModDefenseMagic + mods.m_PartyCombatResist,
        // [StatType.Evade] = mods => mods.m_ModEvadeRating + mods.m_PartyCombatEvade,
    };
    
    public static float GetStatValue(StatType statType, CharacterStats stats) =>
        statType switch
        {
            StatType.Awareness => stats.Awareness,
            StatType.Strength => stats.Toughness,
            StatType.Vitality => stats.Vitality,
            StatType.Speed => stats.Quickness,
            StatType.Intelligence => stats.Fortitude,
            StatType.Luck => stats.Luck,
            StatType.Talent => stats.Talent,
            _ => -1
        };

    public Dictionary<StatType, FittingCharacterStats> GetMaximizeStatLoadout(CharacterOverworld cow)
    {
        var r = this.accessorsMap.ToDictionary(pair => pair.Key,
            pair =>
            {
                var loadout =  InventoryCalculatorHelper.CalculateBestForStat(cow, pair.Key);
                Plugin.Log.DebugCond(() => $"[FITTING] calculation done ({pair.Key})");
                var fit = LoadoutFittingHelper.CreateFittingCharacterStats(cow, loadout);
                Plugin.Log.DebugCond(() => $"[FITTING] fitting done ({pair.Key})");
                Plugin.Log.DebugCond(() => JsonConvert.SerializeObject(loadout));
                return fit;
            });
        
        Plugin.Log.DebugCond(() => {
            var sb = new StringBuilder("[FITTING] done:: ")
                .AppendLine();
            foreach (var key in this.accessorsMap.Keys)
            {
                sb.AppendLine($"  {key:G}: {GetStatValue(key, r[key])}");
            }

            return sb.ToString();
        });
        return r;
    }
}