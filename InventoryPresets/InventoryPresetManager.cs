using System;
using System.Text;
using FullSerializer;
using Google2u;
using GridEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using fsSerializer = On.FullSerializer.fsSerializer;
using LoadoutDict = System.Collections.Generic.Dictionary<PlayerInventory.ContainerID, GridEditor.FTK_itembase.ID>;

namespace AmadarePlugin.InventoryPresets;

public class InventoryPresetManager
{
    public const int ButtonsCount = 5;
    private static Color InnactiveLoadoutColor = new Color(1f, 1f, 0.86f);
    private static Color ActiveLoadoutColor = new Color(0.47f, 0.99f, 1f);
    private static Color NotReadyActiveLoadout = new Color(1f, 0.47f, 0.45f);
    private static Color InnactiveCharacter = new Color(0.29f, 0.14f, 0.14f);

    private GameObject syncObject;
    private GameObject loadoutPanel = null;
    private GameObject[] buttons = new GameObject[ButtonsCount];

    private LoadoutRepository Loadouts = new();
    private InventoryPresetSync sync;

    public void Init()
    {
        syncObject = new GameObject();
        this.sync = this.syncObject.AddComponent<InventoryPresetSync>();
        sync.Manager = this;
        sync.Repository = this.Loadouts;
        
        On.uiPlayerInventory.ShowCharacterInventory += (orig, self, cow, cycler) =>
        {
            orig(self, cow, cycler);
            var rect = (RectTransform)self.gameObject.transform.Find("InventoryBackground").transform;
            if (loadoutPanel == null)
            {
                InitLoadoutButtons(rect);
            }
            else
            {
                this.loadoutPanel.SetActive(true);
            }

            UpdateLoadoutButtonsState(cow);
        };
        On.uiPlayerInventory.OnClose += (orig, self) =>
        {
            orig(self);
            this.loadoutPanel?.SetActive(false);
        };
        On.uiItemMenu.Give += (orig, self, b) =>
        {
            orig(self, b);
            UpdateLoadoutButtonsState(FTKUI.Instance.m_PlayerInventory.m_InventoryOwner);
        };
        On.uiItemMenu.Sell += (orig, self, b) =>
        {
            orig(self, b);
            UpdateLoadoutButtonsState(FTKUI.Instance.m_PlayerInventory.m_InventoryOwner);
        };
        On.FullSerializer.fsSerializer.TrySerialize += FsSerializerOnTrySerialize;
        On.FullSerializer.fsSerializer.TryDeserialize += FsSerializerOnTryDeserialize;
    }

    private fsResult FsSerializerOnTryDeserialize(fsSerializer.orig_TryDeserialize orig, FullSerializer.fsSerializer self, fsData data, Type storagetype, ref object result)
    {
        if (storagetype == typeof(GameSerialize))
        {
            var res = orig(self, data, storagetype, ref result);
            if (res.Succeeded)
            {
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

    private void InitLoadoutButtons(RectTransform parentRect)
    {
        var panel = new GameObject("loadout-panel");
        this.loadoutPanel = panel;
        var transform = panel.AddComponent<RectTransform>();
        transform.SetParent(parentRect);
        
        transform.anchorMin = Vector2.zero;
        transform.anchorMax = Vector2.one;
        transform.anchoredPosition = new Vector2(-290, -400);
        transform.sizeDelta = new Vector2(-570, -700);
        transform.pivot = Vector2.one;

        for (var i = 0; i < 5; i++)
        {
            this.buttons[i] = CreateButton(transform, i);
        }
    }

    private GameObject CreateButton(RectTransform parentRect, int idx)
    {
        var go = new GameObject("loadout-button");
        
        var transform = go.AddComponent<RectTransform>();
        transform.SetParent(parentRect);
        
        transform.anchorMin = new Vector2(0, 1);
        transform.anchorMax = new Vector2(0, 1);
        transform.anchoredPosition = new Vector2(idx * 6f, 0);
        transform.sizeDelta = new Vector2(5, 5);

        var image = go.AddComponent<Image>();
        image.color = Color.white;
        image.raycastTarget = true;

        var button = go.AddComponent<Button>();
        button.interactable = true;
        button.transition = Selectable.Transition.ColorTint;
        button.targetGraphic = image;

        var handler = go.AddComponent<LoadoutButtonClickHandler>();
        handler.Callback = OnClick;
        handler.Index = idx;

        return go;
    }

    private void OnClick(int idx, PointerEventData.InputButton button)
    {
        switch (button)
        {
            case PointerEventData.InputButton.Left:
                LoadSlot(idx);
                break;
            case PointerEventData.InputButton.Right:
                SaveSlot(idx);
                break;
            case PointerEventData.InputButton.Middle:
                ClearSlot(idx);
                break;
        }
    }

    private void LoadSlot(int idx)
    {
        Plugin.Log.LogInfo("Load slot #" + idx);
        var cow = FTKUI.Instance.m_PlayerInventory.m_InventoryOwner;
        var loadout = this.Loadouts.Get(cow.m_PlayerName, idx);
        
        
        // LOG
        var sb = new StringBuilder($"Loadout #{idx}:  ");
        foreach (var pair in loadout)
        {
            sb.Append(pair.Key.ToString("G"))
                .Append(": ");
            sb.Append(FTKHub.Localized<TextItems>("STR_" + pair.Key)).Append(" ");

            sb.AppendLine();
        }
        Plugin.Log.LogInfo(sb.ToString());
        
        // EQUIP
        var inventory = cow.m_PlayerInventory;
        var activeContainers = inventory.m_Containers();

        foreach (var ctId in ValidContainers)
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
        
        Plugin.Log.LogInfo("Loadout equip done!");
    }

    private void ClearSlot(int idx)
    {
        Plugin.Log.LogInfo("Clear slot #" + idx);
        var cow = FTKUI.Instance.m_PlayerInventory.m_InventoryOwner;
        this.Loadouts.Clear(cow.m_PlayerName, idx);
        UpdateLoadoutButtonsState(cow);
        this.sync.SyncLoadouts();
    }

    private void SaveSlot(int idx)
    {
        Plugin.Log.LogInfo("Save slot #" + idx);
        var cow = FTKUI.Instance.m_PlayerInventory.m_InventoryOwner;
        var loadout = GetCurrentLoadout(cow);
        this.Loadouts.Set(cow.m_PlayerName, idx, loadout);
        UpdateLoadoutButtonsState(cow);
        this.sync.SyncLoadouts();
    }

    private void UpdateLoadoutButtonsState(CharacterOverworld cow)
    {
        for (var idx = 0; idx < ButtonsCount; idx++)
        {
            this.buttons[idx].GetComponent<Image>().color = GetButtonColor(cow, idx);
        }
    }

    private Color GetButtonColor(CharacterOverworld cow, int idx)
    {
        bool available = GameLogic.Instance.IsSinglePlayer() && GameLogic.Instance.IsLocalMultiplayer() || cow.IsOwner || cow.m_WaitForRespawn;
        if (!available) return InnactiveCharacter;
        if (this.Loadouts.HasLoadoutAtSlot(cow.m_PlayerName, idx))
        {
            return CheckLoadoutFullyAvailable(this.Loadouts.Get(cow.m_PlayerName, idx), cow)
                ? ActiveLoadoutColor
                : NotReadyActiveLoadout;
        }

        return InnactiveLoadoutColor;
    }

    private bool CheckLoadoutFullyAvailable(LoadoutDict dict, CharacterOverworld cow)
    {
        foreach (var pair in dict)
        {
            if (!HasItem(cow, pair.Value))
            {
                return false;
            }
        }

        return true;
    }

    private bool HasItem(CharacterOverworld cow, FTK_itembase.ID id)
    {
        var inventory = cow.m_PlayerInventory;
        var containers = inventory.m_Containers();
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

    private LoadoutDict GetCurrentLoadout(CharacterOverworld cow)
    {
        var inventory = cow.m_PlayerInventory;
        var containers = inventory.m_Containers();
        var result = new LoadoutDict();
        foreach (var ctId in ValidContainers)
        {
            if (containers.TryGetValue(ctId, out var container) && !container.IsEmpty())
            {
                result[ctId] = container.GetOne();
            }
        }

        return result;
    }

    private static PlayerInventory.ContainerID[] ValidContainers =
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