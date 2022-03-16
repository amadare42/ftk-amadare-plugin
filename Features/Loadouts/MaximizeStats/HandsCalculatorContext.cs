using System;
using System.Collections.Generic;
using System.Linq;
using GridEditor;

namespace AmadarePlugin.Features.Loadouts.MaximizeStats;

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