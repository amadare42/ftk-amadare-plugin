using System;
using System;
using System.Collections.Generic;
using GridEditor;

namespace AmadarePlugin.Common;

public static class CachedDB
{
    private static readonly Dictionary<FTK_itembase.ID, FTK_characterModifier.ID> itemToModMap = new();
    private static readonly Dictionary<FTK_itembase.ID, string> itemIdToStringMap = new();
    private static readonly Dictionary<string, FTK_itembase.ID> nameToItemIdMap = new();

    public static void Init()
    {
        var list = new List<string>();
        
        // item to mod map
        foreach (var name in Enum.GetNames(typeof(FTK_characterModifier.ID)))
        {
            try
            {
                var itemId = (FTK_itembase.ID)Enum.Parse(typeof(FTK_itembase.ID), name, true);
                var modId = (FTK_characterModifier.ID)Enum.Parse(typeof(FTK_characterModifier.ID), name);
                itemToModMap[itemId] = modId;
            }
            catch (Exception ex)
            {
                list.Add(name);
            }
        }
        Plugin.Log.LogInfo($"Cached item to mod mapping. {itemToModMap.Count} items. Not found: {list.Count} ({string.Join(",  ", list.ToArray())}");

        // item to name map
        foreach (var name in Enum.GetNames(typeof(FTK_itembase.ID)))
        {
            try
            {
                var itemId = (FTK_itembase.ID)Enum.Parse(typeof(FTK_itembase.ID), name, true);
                itemIdToStringMap[itemId] = name;
                nameToItemIdMap[name] = itemId;
            }
            catch (Exception ex)
            {
            }
        }
        Plugin.Log.LogInfo($"Cached item to name mapping. {itemIdToStringMap.Count} items.");
    }

    public static FTK_characterModifier.ID ItemToModId(FTK_itembase item)
    {
        return itemToModMap[nameToItemIdMap[item.m_ID]];
    }
    
    public static FTK_characterModifier.ID ItemToModId(FTK_itembase.ID itemId)
    {
        return itemToModMap[itemId];
    }

    public static bool TryGetModById(FTK_characterModifier.ID modId, out FTK_characterModifier mod)
    {
        mod = null;
        var db = FTK_characterModifierDB.GetDB();
        if (db.m_Dictionary == null) 
            return false;

        return db.m_Dictionary.TryGetValue((int)modId, out mod);
    }
    
    public static bool TryGetModByItemId(FTK_itembase.ID itemId, out FTK_characterModifier mod, out FTK_characterModifier.ID modId)
    {
        mod = null;
        
        if (!itemToModMap.TryGetValue(itemId, out modId))
        {
            return false;
        }
        
        var db = FTK_characterModifierDB.GetDB();
        if (db.m_Dictionary == null) 
            return false;

        return db.m_Dictionary.TryGetValue((int)modId, out mod);
    }
    
    public static bool TryGetModByItem(FTK_itembase item, out FTK_characterModifier mod, out FTK_characterModifier.ID modId)
    {
        mod = null;
        modId = default;
        
        if (!nameToItemIdMap.TryGetValue(item.m_ID, out var itemId))
        {
            return false;
        }
        
        if (!itemToModMap.TryGetValue(itemId, out modId))
        {
            return false;
        }
        
        var db = FTK_characterModifierDB.GetDB();
        if (db.m_Dictionary == null) 
            return false;

        return db.m_Dictionary.TryGetValue((int)modId, out mod);
    }

    public static FTK_itembase.ID GetItemId(FTK_itembase item)
    {
        if (item == null || !nameToItemIdMap.TryGetValue(item.m_ID, out var itemId))
        {
            Plugin.Log.LogWarning("GetId: null");
            return FTK_itembase.ID.None;
        }

        return itemId;
    }
}