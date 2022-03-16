using AmadarePlugin.Common;
using Google2u;
using GridEditor;

namespace AmadarePlugin.Extensions;

public static class ItembaseExtensions
{
    public static PlayerInventory.ContainerID GetContainer(this FTK_itembase.ObjectType objectType)
    {
        PlayerInventory.ContainerID id = PlayerInventory.ContainerID.Trinket;
        if (objectType == FTK_itembase.ObjectType.helmet)
            id = PlayerInventory.ContainerID.Head;
        else if (objectType == FTK_itembase.ObjectType.boots)
            id = PlayerInventory.ContainerID.Foot;
        else if (objectType == FTK_itembase.ObjectType.armor)
            id = PlayerInventory.ContainerID.Body;
        else if (objectType == FTK_itembase.ObjectType.necklace)
            id = PlayerInventory.ContainerID.Neck;
        else if (objectType == FTK_itembase.ObjectType.shield)
            id = PlayerInventory.ContainerID.LeftHand;
        else if (objectType == FTK_itembase.ObjectType.weapon)
            id = PlayerInventory.ContainerID.RightHand;

        return id;
    }
    
    public static bool IsTwoHanded(this FTK_itembase item)
    {
        return item.m_ObjectSlot == FTK_itembase.ObjectSlot.twoHands;
    }
    
    public static bool IsRightHanded(this FTK_itembase item)
    {
        return item.m_ObjectSlot == FTK_itembase.ObjectSlot.oneHand && item.m_ObjectType == FTK_itembase.ObjectType.weapon;
    }
    
    public static bool IsLeftHanded(this FTK_itembase item)
    {
        return item.m_ObjectSlot == FTK_itembase.ObjectSlot.oneHand && item.m_ObjectType != FTK_itembase.ObjectType.weapon;
    }
    
    public static bool IsOneHanded(this FTK_itembase item)
    {
        if (item == null)
        {
            Plugin.Log.LogWarning("IsOneHanded: null");
            return false;
        }
        return item.m_ObjectSlot == FTK_itembase.ObjectSlot.oneHand;
    }
    
    public static PlayerInventory.ContainerID OtherHand(this FTK_itembase item)
    {
        if (item.IsRightHanded())
        {
            return PlayerInventory.ContainerID.LeftHand;
        }
        
        return PlayerInventory.ContainerID.RightHand;
    }
    
    public static FTK_itembase.ID GetId(this FTK_itembase item)
    {
        return CachedDB.GetItemId(item);
    }

    public static bool HasItemsIn(this PlayerInventory inventory, PlayerInventory.ContainerID containerId)
    {
        if (inventory.m_Containers.TryGetValue(containerId, out var container))
        {
            return !container.IsEmpty();
        }

        return false;
    } 
    
    public static FTK_itembase GetItemIn(this PlayerInventory inventory, PlayerInventory.ContainerID containerId)
    {
        if (inventory.m_Containers.TryGetValue(containerId, out var container))
        {
            var item = container.GetOne();
            if (item == FTK_itembase.ID.None) return null;
            return FTK_itembase.GetItemBase(item);
        }

        return null;
    } 
    
    public static FTK_itembase.ID GetItemIdIn(this PlayerInventory inventory, PlayerInventory.ContainerID containerId)
    {
        if (inventory.m_Containers.TryGetValue(containerId, out var container))
        {
            return container.GetOne();
        }

        return FTK_itembase.ID.None;
    }

    public static string GetLocalizedName(this FTK_itembase.ID itemId)
    {
        return FTKHub.Localized<TextItems>("STR_" + itemId);
    }
}