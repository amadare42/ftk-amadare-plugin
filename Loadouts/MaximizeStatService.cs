using System;
using System.Collections.Generic;
using System.Linq;
using AmadarePlugin.Loadouts.Fitting;
using GridEditor;
using LoadoutDict = System.Collections.Generic.Dictionary<PlayerInventory.ContainerID, GridEditor.FTK_itembase.ID>;

namespace AmadarePlugin.Loadouts;

public enum StatType
{
    Strength,
    Intelligence,
    Awareness,
    Vitality,
    Speed,
    Talent,
    Luck,
    COUNT
    
    // Armor,
    // Resistance,
    // Evade
}

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

    public Dictionary<StatType, FittingCharacterStats> GetOptimizedLoadouts(CharacterOverworld cow)
    {
        var items = cow.m_PlayerInventory.m_Containers
            .Values
            .Where(c => !c.IsEmpty())
            .SelectMany(c => c.m_ItemCounts.Keys)
            .Where(i => i != FTK_itembase.ID.None)
            .Distinct()
            .Select(FTK_itembase.GetItemBase)
            .Where(item => item != null)
            .Where(itemEntry => itemEntry.m_Equippable)
            .Select(itemEntry =>
            {
                var mod = FTK_characterModifier.GetEnum(itemEntry.m_ID);
                var modEntry = FTK_characterModifierDB.GetDB().GetEntry(mod);
                return new EntryTuple(itemEntry, modEntry);
            })
            .ToArray();

        return this.accessorsMap
            .ToDictionary(pair => pair.Key, pair =>
            {
                var loadout = FindBest(items, pair.Value, cow.m_PlayerInventory);
                return LoadoutFittingHelper.CreateFittingCharacterStats(cow, loadout, true);
            });
    }

    private LoadoutDict ToLoadout(List<EntryTuple> set)
    {
        return set.ToDictionary(e => e.GetContainer(), e => e.GetId());
    }

    public LoadoutDict FindBest(EntryTuple[] items, Func<FTK_characterModifier, float> accessor, PlayerInventory inventory)
    {
        var result = new List<EntryTuple>();
        var simple = new[]
        {
            FTK_itembase.ObjectType.helmet,
            FTK_itembase.ObjectType.boots,
            FTK_itembase.ObjectType.armor,
            FTK_itembase.ObjectType.necklace,
            FTK_itembase.ObjectType.trinket,
        };
        foreach (var type in simple)
        {
            var best = items.Where(i => i.Item.m_ObjectType == type)
                .OrderByDescending(i => accessor(i.Mod))
                .FirstOrDefault();
            if (best != null)
            {
                if (IsSameValueAsEquipped(best, accessor, inventory))
                {
                    var itemId = inventory.m_Containers[best.GetContainer()].GetOne();
                    if (itemId == FTK_itembase.ID.None)
                    {
                        Plugin.Log.LogWarning("Item ID was None during test fit");
                        result.Add(best);
                    }
                    else
                    {
                        result.Add(items.First(e => e.GetId() == itemId));
                    }
                }
                else
                {
                    result.Add(best);
                }
            }
        }
        
        var bestTwoHands = items.Where(i => i.Item.m_ObjectSlot == FTK_itembase.ObjectSlot.twoHands)
            .OrderByDescending(i => accessor(i.Mod))
            .FirstOrDefault();
        
        var bestShield = items.Where(i => i.Item.m_ObjectType == FTK_itembase.ObjectType.shield)
            .OrderByDescending(i => accessor(i.Mod))
            .FirstOrDefault();
        var bestOneHanded = items.Where(i => i.Item.m_ObjectSlot == FTK_itembase.ObjectSlot.oneHand && i.Item.m_ObjectType != FTK_itembase.ObjectType.shield)
            .OrderByDescending(i => accessor(i.Mod))
            .FirstOrDefault();
        var twoItemsScore = (bestShield != null ? accessor(bestShield.Mod) : 0) + (bestOneHanded != null ? accessor(bestOneHanded.Mod) : 0);
        var twoHandsScore = bestTwoHands != null ? accessor(bestTwoHands.Mod) : 0;

        if (twoHandsScore > twoItemsScore)
        {
            if (bestTwoHands != null) result.Add(bestTwoHands);
        }
        else
        {
            if (bestShield != null) result.Add(bestShield);
            if (bestOneHanded != null) result.Add(bestOneHanded);
        }

        var loadout = ToLoadout(result);
        return loadout;
    }

    private bool IsSameValueAsEquipped(EntryTuple best, Func<FTK_characterModifier, float> accessor, PlayerInventory inventory)
    {
        var containerId = best.GetContainer();

        if (inventory.m_Containers.TryGetValue(containerId, out var equipped))
        {
            var equippedItemId = equipped.GetOne();
            if (equippedItemId == FTK_itembase.ID.None)
            {
                return false;
            }
            
            var mod = FTK_characterModifier.GetEnum(equippedItemId.ToString());
            var modEntry = FTK_characterModifierDB.GetDB().GetEntry(mod);
                    
            if (Math.Abs(accessor(modEntry) - accessor(best.Mod)) < 0.0001)
            {
                // equipped item have same value, don't add it to loadout
                return true;
            }
        }

        return false;
    }

    public class EntryTuple
    {
        public FTK_itembase Item { get; }
        public FTK_characterModifier Mod { get; }

        public EntryTuple(FTK_itembase item, FTK_characterModifier mod)
        {
            this.Item = item;
            this.Mod = mod;
        }

        public FTK_itembase.ID GetId()
        {
            return FTK_itembase.GetEnum(this.Item.m_ID);
        }

        public PlayerInventory.ContainerID GetContainer()
        {
            var itemBase = this.Item; 
            
            PlayerInventory.ContainerID id = PlayerInventory.ContainerID.Trinket;
            if (itemBase.m_ObjectType == FTK_itembase.ObjectType.helmet)
                id = PlayerInventory.ContainerID.Head;
            else if (itemBase.m_ObjectType == FTK_itembase.ObjectType.boots)
                id = PlayerInventory.ContainerID.Foot;
            else if (itemBase.m_ObjectType == FTK_itembase.ObjectType.armor)
                id = PlayerInventory.ContainerID.Body;
            else if (itemBase.m_ObjectType == FTK_itembase.ObjectType.necklace)
                id = PlayerInventory.ContainerID.Neck;
            else if (itemBase.m_ObjectType == FTK_itembase.ObjectType.shield)
                id = PlayerInventory.ContainerID.LeftHand;
            else if (itemBase.m_ObjectType == FTK_itembase.ObjectType.weapon)
                id = PlayerInventory.ContainerID.RightHand;

            return id;
        }
    }
}