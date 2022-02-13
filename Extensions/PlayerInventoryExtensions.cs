using System.Collections.Generic;
using AmadarePlugin.Loadouts;

namespace AmadarePlugin.Extensions;

public static class PlayerInventoryExtensions
{
    public static Dictionary<PlayerInventory.ContainerID, ItemContainer> GetValidEquipmentContainers(this PlayerInventory inventory)
    {
        var dictionary = new Dictionary<PlayerInventory.ContainerID, ItemContainer>();
        
        foreach (var ctId in LoadoutManager.EquipableContainers)
        {
            dictionary[ctId] = inventory.Get(ctId);
        }

        return dictionary;
    }
}