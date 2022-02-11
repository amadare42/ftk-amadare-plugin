using System;
using System.Collections.Generic;

namespace AmadarePlugin.InventoryPresets;

public static class FieldsExtensions
{
    private static Func<PlayerInventory, Dictionary<PlayerInventory.ContainerID, ItemContainer>> m_ContainersAccessor;

    public static Dictionary<PlayerInventory.ContainerID, ItemContainer> m_Containers(this PlayerInventory inventory)
    {
        m_ContainersAccessor ??= inventory.GetFieldAccessor<PlayerInventory, Dictionary<PlayerInventory.ContainerID, ItemContainer>>("m_Containers");
        return m_ContainersAccessor(inventory);
    }
}