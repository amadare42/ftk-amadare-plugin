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

public class DistributedLoadout
{
    public Dictionary<PlayerInventory.ContainerID, FTK_itembase.ID> Loadout { get; set; }
    public Dictionary<FTK_itembase.ID, CharacterOverworld> OwnersMap { get; set; }
}