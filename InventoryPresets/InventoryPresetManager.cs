using System;
using System.Linq;
using AmadarePlugin.InventoryPresets.UI;
using FullSerializer;
using GridEditor;
using UnityEngine;
using fsSerializer = On.FullSerializer.fsSerializer;
using LoadoutDict = System.Collections.Generic.Dictionary<PlayerInventory.ContainerID, GridEditor.FTK_itembase.ID>;

namespace AmadarePlugin.InventoryPresets;

public class InventoryPresetManager : ILoadoutButtonsCallbacks
{
    public const int SlotsCount = 5;
    
    private GameObject syncObject;
    private LoadoutRepository Loadouts = new();
    private InventoryPresetSync sync;
    private UiInventoryLoadoutManager ui;

    public void Init()
    {
        InitSyncObject();
        this.ui = new UiInventoryLoadoutManager(this, this.Loadouts);
        
        On.FullSerializer.fsSerializer.TrySerialize += FsSerializerOnTrySerialize;
        On.FullSerializer.fsSerializer.TryDeserialize += FsSerializerOnTryDeserialize;
    }

    private void InitSyncObject()
    {
        syncObject = new GameObject();
        this.sync = this.syncObject.AddComponent<InventoryPresetSync>();
        sync.Manager = this;
        sync.Repository = this.Loadouts;
    }

    private fsResult FsSerializerOnTryDeserialize(fsSerializer.orig_TryDeserialize orig, FullSerializer.fsSerializer self, fsData data, Type storagetype, ref object result)
    {
        if (storagetype == typeof(GameSerialize))
        {
            var res = orig(self, data, storagetype, ref result);
            if (res.Succeeded)
            {
                // try to fetch loadout info each time game state is deserialized
                if (data.AsDictionary.TryGetValue("m_Loadouts", out var entry))
                {
                    var asString = entry.AsString;
                    Plugin.Log.LogInfo("Loading loadouts info " + asString);
                    this.Loadouts.Load(asString);
                }
                else
                {
                    Plugin.Log.LogInfo("Save doesn't have loadout info");
                }
            }

            return res;
        }
        return orig(self, data, storagetype, ref result);
    }

    private fsResult FsSerializerOnTrySerialize(fsSerializer.orig_TrySerialize orig, FullSerializer.fsSerializer self, Type storagetype, object instance, out fsData data)
    {
        // adding loadout info each time game state is serialized
        if (instance is GameSerialize)
        {
            var result = orig(self, storagetype, instance, out data);
            var serialize = this.Loadouts.Serialize();
            data.AsDictionary["m_Loadouts"] = new fsData(serialize);
            Plugin.Log.LogInfo("Saved loadouts info: " + serialize);
            return result;
        }
        
        return orig(self, storagetype, instance, out data);
    }

    public void LoadSlot(int idx)
    {
        Plugin.Log.LogInfo("Load slot #" + idx);
        if (!IsActivePlayer())
        {
            Plugin.Log.LogInfo("Loadout action prevented - wrong player!");
            return;
        }
        var cow = FTKUI.Instance.m_PlayerInventory.m_InventoryOwner;
        var loadout = this.Loadouts.Get(cow.m_PlayerName, idx);

        if (!loadout.Any())
        {
            Plugin.Log.LogInfo("Loadout action prevented - empty loadout!");
            return;
        }
        
        // EQUIP
        var inventory = cow.m_PlayerInventory;
        var activeContainers = inventory.m_Containers();

        foreach (var ctId in EquipableContainers)
        {
            var equipedItemId = activeContainers[ctId].GetOne();
            
            // does loadout have item for this container?
            if (loadout.TryGetValue(ctId, out var id))
            {
                // required item already on?
                if (activeContainers[ctId].GetItemCount(id) >= 1)
                {
                    continue;
                }
                
                var characterHaveItem = activeContainers[PlayerInventory.ContainerID.Backpack].GetItemCount(id) >= 1;
                if (characterHaveItem)
                {
                    // is required slot already occupied
                    if (equipedItemId != FTK_itembase.ID.None)
                    {
                        cow.UnequipItem(equipedItemId, true);
                    }
                    cow.EquipItem(id);
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

    public void ClearSlot(int idx)
    {
        Plugin.Log.LogInfo("Clear slot #" + idx);
        if (!IsActivePlayer())
        {
            Plugin.Log.LogInfo("Loadout action prevented - wrong player!");
            return;
        }
        var cow = FTKUI.Instance.m_PlayerInventory.m_InventoryOwner;
        this.Loadouts.Clear(cow.m_PlayerName, idx);
        this.ui.UpdateLoadoutButtonsState(cow);
        this.sync.SyncLoadouts();
    }

    public void SaveSlot(int idx)
    {
        Plugin.Log.LogInfo("Save slot #" + idx);
        if (!IsActivePlayer())
        {
            Plugin.Log.LogInfo("Loadout action prevented - wrong player!");
            return;
        }
        
        var cow = FTKUI.Instance.m_PlayerInventory.m_InventoryOwner;
        var loadout = GetCurrentLoadout(cow);
        this.Loadouts.Set(cow.m_PlayerName, idx, loadout);
        this.ui.UpdateLoadoutButtonsState(cow);
        this.sync.SyncLoadouts();
    }

    public bool IsActivePlayer()
    {
        var cow = FTKUI.Instance.m_PlayerInventory.m_InventoryOwner;
        return GameLogic.Instance.IsSinglePlayer() && GameLogic.Instance.IsLocalMultiplayer() || cow.IsOwner ||
               cow.m_WaitForRespawn;
        // var currentPlayer = GameLogic.Instance.GetCurrentCOW();
        //
        // return inventoryOwner.m_FTKPlayerID.Equals(currentPlayer.m_FTKPlayerID);
    }

    private static LoadoutDict GetCurrentLoadout(CharacterOverworld cow)
    {
        var inventory = cow.m_PlayerInventory;
        var containers = inventory.m_Containers();
        var result = new LoadoutDict();
        foreach (var ctId in EquipableContainers)
        {
            if (containers.TryGetValue(ctId, out var container) && !container.IsEmpty())
            {
                result[ctId] = container.GetOne();
            }
        }

        return result;
    }

    public static readonly PlayerInventory.ContainerID[] EquipableContainers =
    {
        PlayerInventory.ContainerID.LeftHand,
        PlayerInventory.ContainerID.RightHand,
        PlayerInventory.ContainerID.Head,
        PlayerInventory.ContainerID.Body,
        PlayerInventory.ContainerID.Foot,
        PlayerInventory.ContainerID.Trinket,
        PlayerInventory.ContainerID.Neck,
    };
}