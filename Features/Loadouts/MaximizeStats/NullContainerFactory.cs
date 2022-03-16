using System.Collections.Generic;
using GridEditor;

namespace AmadarePlugin.Features.Loadouts.MaximizeStats;

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