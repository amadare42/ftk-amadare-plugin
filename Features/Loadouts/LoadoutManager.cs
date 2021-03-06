using System.Collections.Generic;
using System.Linq;
using AmadarePlugin.Common;
using AmadarePlugin.Extensions;
using AmadarePlugin.Features.Loadouts.Sync;
using AmadarePlugin.Features.Loadouts.UI;
using GridEditor;
using LoadoutDict = System.Collections.Generic.Dictionary<PlayerInventory.ContainerID, GridEditor.FTK_itembase.ID>;

namespace AmadarePlugin.Features.Loadouts;

public class LoadoutManager : ILoadoutButtonsCallbacks
{
    public const int SlotsCount = 5;
    
    private LoadoutRepository Loadouts;
    private readonly UILoadoutManager ui;
    private AssignToAllLoadoutsButton assignToAllButton;
    private CharacterShareTracker shareTracker;

    public LoadoutManager(CharacterShareTracker shareTracker, LoadoutRepository loadoutRepository)
    {
        this.shareTracker = shareTracker;
        this.Loadouts = loadoutRepository;
        this.ui = new UILoadoutManager(this, this.Loadouts, this.shareTracker);
        this.assignToAllButton = new AssignToAllLoadoutsButton();

        this.assignToAllButton.OnAssignToAllLoadoutsClick += AssignToAllLoadouts;
        On.GameLogic.RestartFadeOutFinish += GameLogicOnRestartFadeOutFinish;
    }

    private void GameLogicOnRestartFadeOutFinish(On.GameLogic.orig_RestartFadeOutFinish orig, GameLogic self)
    {
        orig(self);
        Plugin.Log.LogInfo("Cleared all loadouts");
        this.Loadouts.ClearAll();
        this.shareTracker.ClearAll();
    }

    public void LoadLoadout(CharacterOverworld cow, LoadoutDict loadout, bool isDistributed)
    {
        if (!FtkHelpers.IsActivePlayer)
        {
            Plugin.Log.LogInfo("Loadout action prevented - wrong player!");
            return;
        }
        
        // EQUIP
        var inventory = cow.m_PlayerInventory;
        var activeContainers = inventory.m_Containers;

        var linkedCows = isDistributed
            ? cow.GetLinkedPlayers(false)
                .Where(lp => this.shareTracker.Get(lp.GetCowUniqueName()))
                .ToList()
            : new List<CharacterOverworld>(0);

        foreach (var ctId in FtkHelpers.EquipableContainers)
        {
            var equipedItemId = activeContainers[ctId].GetOne();
            
            // does loadout have item for this container?
            if (loadout.TryGetValue(ctId, out var itemID))
            {
                // required item already on?
                if (activeContainers[ctId].GetItemCount(itemID) >= 1)
                {
                    continue;
                }
                
                var characterHaveItem = activeContainers[PlayerInventory.ContainerID.Backpack].GetItemCount(itemID) >= 1;

                // try get item from linked characters
                if (!characterHaveItem && isDistributed)
                {
                    characterHaveItem = TryGetItemFromLinkedCows(cow, linkedCows, itemID);
                }
                
                if (characterHaveItem)
                {
                    // is required slot already occupied
                    if (equipedItemId != FTK_itembase.ID.None)
                    {
                        cow.UnequipItem(equipedItemId, true);
                    }
                    var item = FTK_itembase.GetItemBase(itemID);
                    if (item.IsTwoHanded() && !cow.m_PlayerInventory.m_ContainerLeftHand.IsEmpty())
                    {
                        cow.UnequipItem(cow.m_PlayerInventory.m_ContainerLeftHand.GetOne(), true);
                    }
                    cow.EquipItem(itemID);
                }

                // equipment is in loadout, but it is missing in inventory
            }
            else
            {
                // unequip item if missing in loadout
                if (equipedItemId != FTK_itembase.ID.None)
                {
                    cow.UnequipItem(equipedItemId, true);
                }
            }
        }
        
        AudioManager.Instance.AudioEvent("Play_gui_ex_equip");
        
        this.ui.UpdateLoadoutButtonsState(cow);
        Plugin.Log.LogInfo("Loadout equip done!");
    }

    private bool TryGetItemFromLinkedCows(CharacterOverworld cow, List<CharacterOverworld> linkedCows, FTK_itembase.ID itemId)
    {
        foreach (var linkedCow in linkedCows)
        {
            // TODO: need to check if inventory of linked character will be updated properly. Especially when multiple items of same kind case
            var backpack = linkedCow.m_PlayerInventory.m_ContainerBackpack;
            if (backpack.GetItemCount(itemId) > 0)
            {
                linkedCow.RemoveItem(PlayerInventory.ContainerID.Backpack, itemId, false);
                cow.AddItemToBackpack(itemId);
                return true;
            }
        }

        return false;
    }

    public void LoadSlot(int idx)
    {
        Plugin.Log.LogInfo("Load slot #" + idx);
        if (!FtkHelpers.IsActivePlayer)
        {
            Plugin.Log.LogInfo("Loadout action prevented - wrong player!");
            return;
        }
        var cow = FTKUI.Instance.m_PlayerInventory.m_InventoryOwner;
        var loadout = this.Loadouts.Get(cow.GetCowUniqueName(), idx);

        if (!loadout.Any())
        {
            Plugin.Log.LogInfo("Loadout action prevented - empty loadout!");
            return;
        }

        LoadLoadout(cow, loadout, false);
    }

    public void ClearSlot(int idx)
    {
        Plugin.Log.LogInfo("Clear slot #" + idx);
        if (!FtkHelpers.IsActivePlayer)
        {
            Plugin.Log.LogInfo("Loadout action prevented - wrong player!");
            return;
        }
        var cow = FTKUI.Instance.m_PlayerInventory.m_InventoryOwner;
        this.Loadouts.Clear(cow.GetCowUniqueName(), idx);
        this.ui.UpdateLoadoutButtonsState(cow);
        LoadoutSync.Current.PushCharacterLoadouts(cow.GetCowUniqueName());
    }

    public void SaveSlot(int idx)
    {
        Plugin.Log.LogInfo("Save slot #" + idx);
        if (!FtkHelpers.IsActivePlayer)
        {
            Plugin.Log.LogInfo("Loadout action prevented - wrong player!");
            return;
        }
        
        var cow = FTKUI.Instance.m_PlayerInventory.m_InventoryOwner;
        var loadout = GetCurrentLoadout(cow);
        this.Loadouts.Set(cow.GetCowUniqueName(), idx, loadout);
        this.ui.UpdateLoadoutButtonsState(cow);
        LoadoutSync.Current.PushCharacterLoadouts(cow.GetCowUniqueName());
    }

    private static LoadoutDict GetCurrentLoadout(CharacterOverworld cow)
    {
        var inventory = cow.m_PlayerInventory;
        var containers = inventory.m_Containers;
        var result = new LoadoutDict();
        foreach (var ctId in FtkHelpers.EquipableContainers)
        {
            if (containers.TryGetValue(ctId, out var container) && !container.IsEmpty())
            {
                result[ctId] = container.GetOne();
            }
        }

        return result;
    }
    
    public void AssignToAllLoadouts(FTK_itembase item)
    {
        var loadouts = this.Loadouts.GetAllSlots(FtkHelpers.InventoryOwnerUniqueName);
        var containerID = item.m_ObjectType.GetContainer();
        foreach (var loadout in loadouts)
        {
            // loadout is empty
            if (!loadout.Any())
                continue;

            if (loadout.TryGetValue(containerID, out var currentItemId))
            {
                var currentItem = FTK_itembase.GetItemBase(currentItemId);
                if (item.IsTwoHanded() && currentItem.IsOneHanded())
                {
                    loadout.Remove(currentItem.OtherHand());
                }
            }

            loadout[containerID] = item.GetId();
        }

        this.Loadouts.SetAllSlots(FtkHelpers.InventoryOwnerUniqueName, loadouts);
        LoadoutSync.Current.PushCharacterLoadouts(FtkHelpers.InventoryOwnerUniqueName);
        AudioManager.Instance.AudioEvent("Play_gui_ex_equip");
        this.ui.UpdateLoadoutButtonsState(FtkHelpers.InventoryOwnerCow);
    }

    public void OnLoadoutReceived()
    {
        if (FtkHelpers.IsInventoryOpen)
        {
            this.ui.UpdateLoadoutButtonsState(FtkHelpers.InventoryOwnerCow);
            Plugin.Log.LogDebug("Updated inventory due to sync");
        }
    }
    
    public void OnShareChanged()
    {
        if (FtkHelpers.IsInventoryOpen)
        {
            this.ui.UpdateShareCheckbox();
            Plugin.Log.LogDebug("Updated inventory due to sync");
        }
    }
}