using System;
using System.Collections.Generic;
using System.Linq;
using GridEditor;

namespace AmadarePlugin.Features.Loadouts.MaximizeStats;

public class MaximizeStatCalculator
{
    public void Calculate(HandsCalculatorContext ctx)
    {
        CalculateSimpleEquip(ctx);
        CalculateWeapons(ctx);
    }
    
    public void CalculateSimpleEquip(HandsCalculatorContext ctx)
    {
        var nonHandContainers = new[]
        {
            PlayerInventory.ContainerID.Head,
            PlayerInventory.ContainerID.Foot,
            PlayerInventory.ContainerID.Body,
            PlayerInventory.ContainerID.Neck,
            PlayerInventory.ContainerID.Trinket,
        };
        foreach (var containerId in nonHandContainers)
        {
            var bestItem = ctx.BestOf(ctx.AllItems.Where(i => i.ContainerId == containerId));
            var equippedItem = ctx.SelectOne(containerId, ctx.EquippedItems);
            if (bestItem.Score > equippedItem.Score)
            {
                ctx.Add(bestItem);
            }
            else if (equippedItem.Score >= 0)
            {
                ctx.Add(equippedItem);
            }
        }
    }
    
    public void CalculateWeapons(HandsCalculatorContext ctx)
    {
        var current = ctx.Group(ctx.EquippedItems.Where(i => i.IsLeft() || i.IsRight()));
        var twoHanded = ctx.BestOf(ctx.AllItems.Where(i => i.IsTwoHanded));
        var bestLeft = ctx.BestOf(ctx.AllItems.Where(i => i.IsLeft()));
        var bestRight = ctx.BestOf(ctx.AllItems.Where(i => i.IsRight() && i.IsOneHanded));
        var oneHandedCombo = ctx.Group(bestLeft, bestRight);

        if (oneHandedCombo.IsEmpty && twoHanded.IsEmpty)
        {
            ctx.Add(current);
            return;
        }
        
        if (!twoHanded.IsEmpty && twoHanded.BetterThan(current, oneHandedCombo))
        {
            ctx.Add(twoHanded);
            return;
        }

        if (oneHandedCombo.BetterThan(current))
        {
            ctx.Add(
                ctx.BestOf(current.Left, oneHandedCombo.Left)
            );

            if (current.IsTwoHanded)
            {
                ctx.Add(oneHandedCombo.Right);
            }
            else
            {
                ctx.Add(
                    ctx.BestOf(current.Right, oneHandedCombo.Right)
                );
            }
            return;
        }
        
        ctx.Add(current);
    }
}

public static class ItemCalculatorExtensions
{
    public static bool BetterThan(this IItemContainer container, params IItemContainer[] others)
    {
        return others.All(o => container.Score > o.Score);
    }

    public static IItemContainer BestOf(this HandsCalculatorContext ctx, params IItemContainer[] items) =>
        ctx.BestOf(items);

    public static bool IsLeft(this IItemContainer item) => item.ContainerId == PlayerInventory.ContainerID.LeftHand;
    public static bool IsRight(this IItemContainer item) => item.ContainerId == PlayerInventory.ContainerID.RightHand;
    public static IItemContainer Group(this HandsCalculatorContext ctx, params IItemContainer[] items) => ctx.Group(items);
}

public interface IItemContainer
{
    PlayerInventory.ContainerID? ContainerId { get; }
    public FTK_itembase.ID ItemId { get; }
    
    IItemContainer[] Items { get; }
    float Score { get; }
    bool IsEmpty { get; }
    IItemContainer Left { get; }
    IItemContainer Right { get; }
    bool IsTwoHanded { get; }
    bool IsOneHanded { get; }
}

public class BaseItemContainer : IItemContainer
{
    public PlayerInventory.ContainerID? ContainerId { get; set; }
    public FTK_itembase.ID ItemId { get; set; }
    public IItemContainer[] Items { get; set; }
    public float Score { get; set; }
    public bool IsEmpty { get; set; }
    public IItemContainer Left { get; set; }
    public IItemContainer Right { get; set; }
    public bool IsTwoHanded { get; set; }
    public bool IsOneHanded { get; set; }

    public override string ToString()
    {
        if (this.IsEmpty)
        {
            return $"[Empty {this.ContainerId:G}]";
        }

        return $"[{this.ContainerId:G}] [{this.Items.Length}]: {this.Score} {this.ItemId}({(int)this.ItemId}) {(this.IsTwoHanded ? "TH" : "")} {(this.IsOneHanded ? "SH" : "")}";
    }
}

public class AggregateItem : IItemContainer
{
    public PlayerInventory.ContainerID? ContainerId { get; }
    public FTK_itembase.ID ItemId { get; }
    public IItemContainer[] Items { get; }
    public float Score { get; }
    public bool IsEmpty => this.Items.Length == 0;
    public IItemContainer Left { get; }
    public IItemContainer Right { get; }
    public bool IsTwoHanded { get; }
    public bool IsOneHanded { get; }

    public AggregateItem(IItemContainer[] items, NullContainerFactory nullFactory) : this(items, nullFactory, null)
    {
        var containerIds = items.Select(i => i.ContainerId)
            .Where(c => c != null)
            .Distinct()
            .ToArray();
        if (containerIds.Length == 1)
        {
            this.ContainerId = containerIds[0];
        }
    }
    
    public AggregateItem(IItemContainer[] items, NullContainerFactory nullFactory, PlayerInventory.ContainerID? containerID)
    {
        this.Items = items;
        this.Score = items.Sum(i => i.Score);
        this.Left = items.FirstOrDefault(i => i.IsLeft()) ?? nullFactory.GetNullContainerFor(PlayerInventory.ContainerID.LeftHand);
        this.Right = items.FirstOrDefault(i => i.IsRight()) ?? nullFactory.GetNullContainerFor(PlayerInventory.ContainerID.RightHand);
        this.IsTwoHanded = items.All(i => i.IsTwoHanded);
        this.IsOneHanded = items.All(i => i.IsOneHanded);
        
        var itemIds = items.Select(i => i.ItemId)
            .Distinct()
            .ToArray();
        if (itemIds.Length == 1)
        {
            this.ItemId = itemIds[0];
        }

        this.ContainerId = containerID;
    }
    
    public override string ToString()
    {
        if (this.IsEmpty)
        {
            return $"[Empty {this.ContainerId:G}]";
        }

        return $"[{this.ContainerId:G}] [{this.Items.Length}]: {this.Score} {this.ItemId}({(int)this.ItemId}) {(this.IsTwoHanded ? "TH" : "")} {(this.IsOneHanded ? "SH" : "")}";
    }
}

public class InventoryCalculatorHelper
{
    private static MaximizeStatCalculator calculator = new();
    private static HandsCalculatorContextFactory contextFactory = new();
    private static readonly Dictionary<StatType, Func<FTK_characterModifier, float>> accessorsMap = new()
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
    
    public static DistributedLoadout CalculateBestForStat(CharacterOverworld cow, List<CharacterOverworld> linkedCows, StatType statType)
    {
        var accessor = accessorsMap[statType];
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

public class DistributedLoadout
{
    public Dictionary<PlayerInventory.ContainerID, FTK_itembase.ID> Loadout { get; set; }
    public Dictionary<FTK_itembase.ID, CharacterOverworld> OwnersMap { get; set; }
}