using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using AmadarePlugin.Extensions;
using AmadarePlugin.Features.Loadouts.Fitting;
using GridEditor;
using Newtonsoft.Json;

namespace AmadarePlugin.Features.Loadouts.MaximizeStats;

public class InventoryCalculatorHelper
{
    private static MaximizeStatCalculator calculator = new();
    private static HandsCalculatorContextFactory contextFactory = new();
    public static readonly Dictionary<StatType, Func<FTK_characterModifier, float>> StatModAccessorMap = new()
    {
        [StatType.Strength] = mods => mods?.m_ModToughness ?? 0,
        [StatType.Intelligence] = mods => mods?.m_ModFortitude ?? 0,
        [StatType.Awareness] = mods => mods?.m_ModAwareness ?? 0,
        [StatType.Vitality] = mods => mods?.m_ModVitality ?? 0,
        [StatType.Speed] = mods => mods?.m_ModQuickness ?? 0,
        [StatType.Talent] = mods => mods?.m_ModTalent ?? 0,
        [StatType.Luck] = mods => mods?.m_ModLuck ?? 0,
        [StatType.Crit] = mods => mods?.m_ModCritChance ?? 0,
        [StatType.GoldMultiplayer] = mods => mods?.m_ModGold ?? 0
        
        // [StatType.Armor] = mods => mods.m_ModDefensePhysical + mods.m_PartyCombatArmor,
        // [StatType.Resistance] = mods => mods.m_ModDefenseMagic + mods.m_PartyCombatResist,
        // [StatType.Evade] = mods => mods.m_ModEvadeRating + mods.m_PartyCombatEvade,
    };
    
    public static Dictionary<StatType, string> StatToIconMap = new()
    {
        [StatType.Strength] = "iconStrength",
        [StatType.Intelligence] = "iconIntelligence",
        [StatType.Awareness] = "iconAwareness",
        [StatType.Vitality] = "iconVitality",
        [StatType.Speed] = "iconSpeed",
        [StatType.Talent] = "iconTalent",
        [StatType.Luck] = "iconLuck",
        [StatType.Crit] = "iconCrit",
        [StatType.GoldMultiplayer] = "coins",

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
            StatType.Crit => stats.ChanceToCrit,
            StatType.GoldMultiplayer => stats.m_ModGold,
            _ => -1
        };
    
    public static Dictionary<StatType, FittingCharacterStats> GetMaximizeStatLoadout(CharacterOverworld cow, List<CharacterOverworld> linkedCows)
    {
        var r = StatModAccessorMap.ToDictionary(pair => pair.Key,
            pair =>
            {
                var distributedLoadout =  CalculateBestForStat(cow, linkedCows, pair.Key);
                Plugin.Log.DebugCond(() => $"[FITTING] calculation done ({pair.Key})");
                var fit = LoadoutFittingHelper.CreateFittingCharacterStats(cow, distributedLoadout);
                Plugin.Log.DebugCond(() => $"[FITTING] fitting done ({pair.Key})");
                Plugin.Log.DebugCond(() => JsonConvert.SerializeObject(distributedLoadout));
                return fit;
            });
        
        Plugin.Log.DebugCond(() => {
            var sb = new StringBuilder("[FITTING] done:: ")
                .AppendLine();
            foreach (var key in InventoryCalculatorHelper.StatModAccessorMap.Keys)
            {
                sb.AppendLine($"  {key:G}: {GetStatValue(key, r[key])}");
            }

            return sb.ToString();
        });
        return r;
    }
    
    public static DistributedLoadout CalculateBestForStat(CharacterOverworld cow, List<CharacterOverworld> linkedCows, StatType statType)
    {
        var accessor = StatModAccessorMap[statType];
        var ctx = contextFactory.Create(cow, linkedCows, accessor, statType);
        calculator.Calculate(ctx);
        var distributedLoadout = new DistributedLoadout
        {
            Loadout = ctx.Loadout,
            OwnersMap = CreateOwnersMap(cow, linkedCows, ctx.Loadout)
        };
        return distributedLoadout;
    }

    private static Dictionary<FTK_itembase.ID, CharacterOverworld> CreateOwnersMap(CharacterOverworld cow, List<CharacterOverworld> linkedCows, Dictionary<PlayerInventory.ContainerID, FTK_itembase.ID> loadout)
    {
        var ownersMap = new Dictionary<FTK_itembase.ID, CharacterOverworld>(loadout.Count);
        foreach (var pair in loadout)
        {
            var itemId = pair.Value;
            
            // NOTE: this is working only for backpack items
            ownersMap[itemId] = cow.HasInventoryItem(itemId)
                ? cow
                : linkedCows.FirstOrDefault(c => c.HasInventoryItem(itemId));
        }

        return ownersMap;
    }
    
}