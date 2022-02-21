using System;
using System.Collections.Generic;
using System.Linq;
using GridEditor;
using Newtonsoft.Json;

namespace AmadarePlugin.Loadouts;

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
        if (IsEmpty)
        {
            return $"[Empty {ContainerId:G}]";
        }

        return $"[{ContainerId:G}] [{Items.Length}]: {Score} {ItemId}({(int)ItemId}) {(IsTwoHanded ? "TH" : "")} {(IsOneHanded ? "SH" : "")}";
    }
}

public class AggregateItem : IItemContainer
{
    public PlayerInventory.ContainerID? ContainerId { get; }
    public FTK_itembase.ID ItemId { get; }
    public IItemContainer[] Items { get; }
    public float Score { get; }
    public bool IsEmpty => Items.Length == 0;
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
            ContainerId = containerIds[0];
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
            ItemId = itemIds[0];
        }

        ContainerId = containerID;
    }
    
    public override string ToString()
    {
        if (IsEmpty)
        {
            return $"[Empty {ContainerId:G}]";
        }

        return $"[{ContainerId:G}] [{Items.Length}]: {Score} {ItemId}({(int)ItemId}) {(IsTwoHanded ? "TH" : "")} {(IsOneHanded ? "SH" : "")}";
    }
}

public class InventoryCalculatorHelper
{
    private static MaximizeStatCalculator calculator = new();
    private static HandsCalculatorContextFactory contextFactory = new();
    private static Dictionary<StatType, Func<FTK_characterModifier, float>> accessorsMap = new()
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
    
    public static Dictionary<PlayerInventory.ContainerID, FTK_itembase.ID> CalculateBestForStat(CharacterOverworld cow, StatType statType)
    {
        var accessor = accessorsMap[statType];
        var ctx = contextFactory.Create(cow, accessor, statType);
        calculator.Calculate(ctx);
        return ctx.Loadout;
    }
}

public class NullContainerFactory
{
    private Dictionary<PlayerInventory.ContainerID?, IItemContainer> cache = new();
    private static IItemContainer NoContainer { get; set; }
    private static IItemContainer[] EmptyArray = new IItemContainer[0];

    static NullContainerFactory()
    {
        NoContainer = InternalCreate(null);
    }

    public NullContainerFactory()
    {
        var c = InternalCreate(PlayerInventory.ContainerID.Backpack);
        c.ContainerId = null;
        NoContainer = c;
    }

    public IItemContainer GetNullContainerFor(PlayerInventory.ContainerID? containerID = null)
    {
        if (containerID == null)
        {
            return NoContainer;
        }
        
        if (this.cache.TryGetValue(containerID, out var item))
        {
            return item;
        }

        var c = InternalCreate(containerID);
        this.cache[containerID] = c;
        return c;
    }

    private static BaseItemContainer InternalCreate(PlayerInventory.ContainerID? containerID)
    {
        var c = new BaseItemContainer();
        c.Left = c;
        c.Right = c;
        c.Score = 0;
        c.ContainerId = containerID;
        c.IsEmpty = true;
        c.ItemId = FTK_itembase.ID.None;
        c.IsOneHanded = false;
        c.IsTwoHanded = false;
        c.Items = EmptyArray;
        return c;
    }
}

public class HandsCalculatorContextFactory
{
    private Dictionary<int, BaseItemContainer> itemsCache = new();
    private readonly NullContainerFactory nullFactory = new();

    private static int ItemHash(StatType stat, FTK_itembase.ID itemId)
    {
        return ((int)stat << 20) + (int)itemId;
    }
    
    public HandsCalculatorContext Create(CharacterOverworld cow, Func<FTK_characterModifier, float> accessor,
        StatType statType)
    {
        float GetItemScore(FTK_itembase itembase)
        {
            if (FTK_characterModifierDB.GetDB().IsContainID(itembase.m_ID))
            {
                var mod = FTK_characterModifier.GetEnum(itembase.m_ID);
                var modEntry = FTK_characterModifierDB.GetDB().GetEntry(mod);
                return accessor(modEntry);
            }

            return 0;
        }
        
        var ctxContainersAll = new List<IItemContainer>();
        var ctxContainersEquipped = new List<IItemContainer>();

        var inventoryContainers = cow.m_PlayerInventory.m_Containers;
        foreach (var pair in inventoryContainers)
        {
            var containerKey = pair.Key;
            var container = pair.Value;
            if (container.IsEmpty())
            {
                continue;
            }
            
            foreach (var itemPairs in container.m_CountDictionary)
            {
                var itemId = itemPairs.Key;
                var item = FTK_itembase.GetItemBase(itemId);
                if (item is not { m_Equippable: true })
                {
                    continue;
                }

                var hash = ItemHash(statType, itemId);
                var targetContainer = item.m_ObjectType.GetContainer();
                if (!this.itemsCache.TryGetValue(hash, out var c))
                {
                    c = new BaseItemContainer
                    {
                        ContainerId = targetContainer,
                        IsOneHanded = item.IsOneHanded(),
                        IsTwoHanded = item.IsTwoHanded(),
                        Score = GetItemScore(item),
                        IsEmpty = false,
                        ItemId = itemId
                    };
                    c.Items = new[] { c };
                    c.Left = targetContainer == PlayerInventory.ContainerID.LeftHand ? c : this.nullFactory.GetNullContainerFor(PlayerInventory.ContainerID.LeftHand);
                    c.Right = targetContainer == PlayerInventory.ContainerID.RightHand ? c : this.nullFactory.GetNullContainerFor(PlayerInventory.ContainerID.RightHand);
                    
                    this.itemsCache[hash] = c;
                }
                
                ctxContainersAll.Add(c);
                if (containerKey == targetContainer)
                {
                    ctxContainersEquipped.Add(c);
                }
            }
        }

        // var json = JsonConvert.SerializeObject(new { all = ctxContainersAll, equipped = ctxContainersEquipped }, Formatting.None, new JsonSerializerSettings()
        // {
        //     PreserveReferencesHandling = PreserveReferencesHandling.Objects
        // });
        // Plugin.Log.LogInfo($"[MAXIMIZE] {json}");
        return new HandsCalculatorContext(this.nullFactory, ctxContainersAll, ctxContainersEquipped);
    }
}

public class HandsCalculatorContext
{
    private readonly NullContainerFactory nullFactory;
    public List<IItemContainer> AllItems { get; }
    public List<IItemContainer> EquippedItems { get; }

    public Dictionary<PlayerInventory.ContainerID, FTK_itembase.ID> Loadout = new();

    public HandsCalculatorContext(NullContainerFactory nullFactory, List<IItemContainer> allItems, List<IItemContainer> equippedItems)
    {
        this.nullFactory = nullFactory;
        this.AllItems = allItems;
        this.EquippedItems = equippedItems;
    }

    public void Add(IItemContainer container)
    {
        foreach (var item in container.Items)
        {
            if (item.ContainerId != null)
            {
                this.Loadout[item.ContainerId.Value] = item.ItemId;
            }
        }
    }

    public IItemContainer Group(IEnumerable<IItemContainer> items) => new AggregateItem(items.ToArray(), this.nullFactory);
    public IItemContainer SelectOne(PlayerInventory.ContainerID container, IEnumerable<IItemContainer> items)
    {
        var filteredItems = items.Where(i => i.ContainerId == container).Take(1).ToArray();
        if (filteredItems.Length == 0)
        {
            return this.nullFactory.GetNullContainerFor(container);
        }
        
        return new AggregateItem(filteredItems, this.nullFactory, container);
    }

    public IItemContainer BestOf<T>(IEnumerable<T> items) where T : IItemContainer
    {
        
        
        var arr = items.ToArray();
        var item = arr.OrderByDescending(i => i.Score)
            .FirstOrDefault();
        if (item == null)
        {
            return this.nullFactory.GetNullContainerFor();
        }

        if (Math.Abs(item.Score - arr[0].Score) < 0.0001)
        {
            item = arr[0];
        }

        if (item.Score < 0)
        {
            return this.nullFactory.GetNullContainerFor(item.ContainerId);
        }

        return item;
    }
}