using System.Linq;
using AmadarePlugin.Loadouts.Sync;
using AmadarePlugin.Loadouts.UI;
using GridEditor;
using LoadoutDict = System.Collections.Generic.Dictionary<PlayerInventory.ContainerID, GridEditor.FTK_itembase.ID>;

namespace AmadarePlugin.Loadouts;

public class LoadoutManager : ILoadoutButtonsCallbacks
{
    public const int SlotsCount = 5;
    
    private LoadoutRepository Loadouts = new();
    private SyncService sync;
    private UILoadoutManager ui;
    private GameSaveInterceptor gameSaveInterceptor;
    private AssignToAllLoadoutsButton assignToAllButton;
    
    public void Init()
    {
        this.sync = new SyncService(this.Loadouts, this);
        this.ui = new UILoadoutManager(this, this.Loadouts);
        this.gameSaveInterceptor = new GameSaveInterceptor(this.Loadouts);
        this.assignToAllButton = new AssignToAllLoadoutsButton();

        this.assignToAllButton.OnAssignToAllLoadoutsClick += AssignToAllLoadouts;
        On.GameLogic.RestartFadeOutFinish += GameLogicOnRestartFadeOutFinish;
        On.uiPlayerInventory.ShowCharacterInventory += (orig, self, cow, cycler) =>
        {
            orig(self, cow, cycler);
            if (this.sync.SyncRequired)
            {
                Plugin.Log.LogInfo("Sync required");
                this.sync.RequestSync();
            }
        };
    }

    private void GameLogicOnRestartFadeOutFinish(On.GameLogic.orig_RestartFadeOutFinish orig, GameLogic self)
    {
        orig(self);
        Plugin.Log.LogInfo("Cleared all loadouts");
        this.Loadouts.ClearAll();
    }

    public void LoadLoadout(CharacterOverworld cow, LoadoutDict loadout)
    {
        if (!IsActivePlayer())
        {
            Plugin.Log.LogInfo("Loadout action prevented - wrong player!");
            return;
        }
        
        // EQUIP
        var inventory = cow.m_PlayerInventory;
        var activeContainers = inventory.m_Containers;

        foreach (var ctId in HelperProperties.EquipableContainers)
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

        LoadLoadout(cow, loadout);
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
        
        var isControllable = GameLogic.Instance.IsSinglePlayer() && GameLogic.Instance.IsLocalMultiplayer() || cow.IsOwner ||
               cow.m_WaitForRespawn;
        return isControllable && IsActiveCow();
    }

    private bool IsActiveCow()
    {
        return GameLogic.Instance.GetCurrentCOW().m_FTKPlayerID.PhotonID == GameLogic.Instance.m_CurrentPlayer.PhotonID;
    }

    private static LoadoutDict GetCurrentLoadout(CharacterOverworld cow)
    {
        var inventory = cow.m_PlayerInventory;
        var containers = inventory.m_Containers;
        var result = new LoadoutDict();
        foreach (var ctId in HelperProperties.EquipableContainers)
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
        var loadouts = this.Loadouts.GetAllSlots(HelperProperties.InventoryOwnerName);
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

        this.Loadouts.SetAllSlots(HelperProperties.InventoryOwnerName, loadouts);
        this.sync.SyncLoadouts();
        AudioManager.Instance.AudioEvent("Play_gui_ex_equip");
        this.ui.UpdateLoadoutButtonsState(HelperProperties.InventoryOwner);
    }
}