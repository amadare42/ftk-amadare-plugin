using System.Linq;
using AmadarePlugin.Extensions;
using AmadarePlugin.Loadouts.UI.Behaviors;
using GridEditor;

namespace AmadarePlugin.Loadouts;
using LoadoutDict = System.Collections.Generic.Dictionary<PlayerInventory.ContainerID, GridEditor.FTK_itembase.ID>;
using ContainersDict = System.Collections.Generic.Dictionary<PlayerInventory.ContainerID, ItemContainer>;

public class LoadoutStateEvaluator
{
    public static LoadoutButtonState GetButtonState(CharacterOverworld cow, LoadoutRepository loadouts, int idx)
    {
        bool isEnabled = GameLogic.Instance.IsSinglePlayer() && GameLogic.Instance.IsLocalMultiplayer() || cow.IsOwner || cow.m_WaitForRespawn;
        if (!isEnabled) return LoadoutButtonState.Disabled;
        
        if (loadouts.HasLoadoutAtSlot(cow.m_PlayerName, idx))
        {
            var containers = cow.m_PlayerInventory.m_Containers;
            var loadout = loadouts.Get(cow.m_PlayerName, idx);
            var isFilled = HasAllItems(loadout, containers);
            if (isFilled)
            {
                var equipmentContainers = cow.m_PlayerInventory.GetValidEquipmentContainers();
                if (IsLoadoutCurrentlyEquipped(loadout, equipmentContainers))
                {
                    return LoadoutButtonState.Equipped;
                }
                
                return LoadoutButtonState.Filled;
            }

            return LoadoutButtonState.Unavailable;
        }

        return LoadoutButtonState.Empty;
    }
    
    public static bool IsLoadoutCurrentlyEquipped(LoadoutDict dict, ContainersDict equippedContainers)
    {
        foreach (var pair in equippedContainers)
        {
            var ctId = pair.Key;
            var container = pair.Value;

            if (dict.TryGetValue(ctId, out var loadoutItemId))
            {
                // if container contains item other that one from loadout, consider equipment different than loadout one 
                if (container.GetOne() != loadoutItemId)
                {
                    return false;
                }
            }
            // if container is occupied but isn't listed in loadout, consider equipment different than loadout one 
            else if (container.GetOne() != FTK_itembase.ID.None)
            {
                return false;
            }
        }

        return true;
    }
    
    public static bool HasAllItems(LoadoutDict dict, ContainersDict containers) => dict.All(pair => HasItem(containers, pair.Value));
    
    public static bool HasItem(ContainersDict containers, FTK_itembase.ID id)
    {
        foreach (var pair in containers)
        {
            var container = pair.Value;
            if (container.GetItemCount(id) >= 1)
            {
                return true;
            }
        }

        return false;
    }
}