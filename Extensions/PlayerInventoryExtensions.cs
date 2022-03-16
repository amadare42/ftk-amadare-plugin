using System.Collections.Generic;
using AmadarePlugin.Common;

namespace AmadarePlugin.Extensions;

public static class PlayerInventoryExtensions
{
    public static Dictionary<PlayerInventory.ContainerID, ItemContainer> GetValidEquipmentContainers(this PlayerInventory inventory)
    {
        var dictionary = new Dictionary<PlayerInventory.ContainerID, ItemContainer>();
        
        foreach (var ctId in FtkHelpers.EquipableContainers)
        {
            dictionary[ctId] = inventory.Get(ctId);
        }

        return dictionary;
    }
}