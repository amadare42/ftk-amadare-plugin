using System.Collections.Generic;
using System.Linq;
using AmadarePlugin.Resources;
using GridEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using LoadoutDict = System.Collections.Generic.Dictionary<PlayerInventory.ContainerID, GridEditor.FTK_itembase.ID>;
using ContainersDict = System.Collections.Generic.Dictionary<PlayerInventory.ContainerID, ItemContainer>;

namespace AmadarePlugin.InventoryPresets.UI;

public class UiInventoryLoadoutManager
{
    private readonly ILoadoutButtonsCallbacks callbacks;
    private readonly LoadoutRepository loadouts;
    private GameObject loadoutPanel = null;
    private LoadoutButton[] buttons = new LoadoutButton[InventoryPresetManager.SlotsCount];
    private Dictionary<LoadoutButtonState, ButtonStateSprites> ButtonStatesMap = new();
    
    public UiInventoryLoadoutManager(ILoadoutButtonsCallbacks callbacks, LoadoutRepository loadouts)
    {
        this.callbacks = callbacks;
        this.loadouts = loadouts;
        On.uiPlayerInventory.ShowCharacterInventory += (orig, self, cow, cycler) =>
        {
            orig(self, cow, cycler);
            var rect = (RectTransform)self.gameObject.transform.Find("InventoryBackground").transform;
            if (this.loadoutPanel == null)
            {
                InitLoadoutButtons(rect);
            }
            else
            {
                this.loadoutPanel.SetActive(true);
            }

            UpdateLoadoutButtonsState(FTKUI.Instance.m_PlayerInventory.m_InventoryOwner);
        };
        On.uiPlayerInventory.OnClose += (orig, self) =>
        {
            orig(self);
            this.loadoutPanel?.SetActive(false);
        };
        On.uiItemMenu.Give += (orig, self, b) =>
        {
            orig(self, b);
            if (this.loadoutPanel.activeSelf)
            {
                UpdateLoadoutButtonsState(FTKUI.Instance.m_PlayerInventory.m_InventoryOwner);
            }
        };
        On.CharacterOverworld.AddItemToBackpackRPC += (orig, self, item, hud) =>
        {
            orig(self, item, hud);
            if (this.loadoutPanel.activeSelf)
            {
                UpdateLoadoutButtonsState(FTKUI.Instance.m_PlayerInventory.m_InventoryOwner);
            }
        };
        On.CharacterOverworld.RemoveItemRPC += (orig, self, id, item, consumed) =>
        {
            orig(self, id, item, consumed);
            if (this.loadoutPanel.activeSelf)
            {
                UpdateLoadoutButtonsState(FTKUI.Instance.m_PlayerInventory.m_InventoryOwner);
            }
        };
        On.CharacterOverworld.UnequipItemRPC += (orig, self, item, _moveToBackpack) =>
        {
            orig(self, item, _moveToBackpack);
            if (this.loadoutPanel.activeSelf)
            {
                UpdateLoadoutButtonsState(FTKUI.Instance.m_PlayerInventory.m_InventoryOwner);
            }
        };
    }
    
    private void InitLoadoutButtons(RectTransform parentRect)
    {
        InitButtonStatesMap();
        
        var panel = new GameObject("loadout-panel");
        this.loadoutPanel = panel;
        var transform = panel.AddComponent<RectTransform>();
        transform.SetParent(parentRect);
        
        transform.anchorMin = Vector2.zero;
        transform.anchorMax = Vector2.one;
        transform.anchoredPosition = new Vector2(-290, -400);
        transform.sizeDelta = new Vector2(-570, -700);
        transform.pivot = Vector2.one;

        for (var i = 0; i < InventoryPresetManager.SlotsCount; i++)
        {
            this.buttons[i] = CreateButton(transform, i);
        }
    }

    private void InitButtonStatesMap()
    {
        Sprite Sprite(string name) => ResourcesManager.Get<Sprite>($"buttons/{name}.png");

        this.ButtonStatesMap[LoadoutButtonState.None] = new ButtonStateSprites(null);
        this.ButtonStatesMap[LoadoutButtonState.Disabled] = new ButtonStateSprites(Sprite("disabled"));
        this.ButtonStatesMap[LoadoutButtonState.Empty] = new ButtonStateSprites(
            Sprite("empty_normal"),
            Sprite("empty_hover"),
            Sprite("empty_pressed")
        );
        this.ButtonStatesMap[LoadoutButtonState.Filled] = new ButtonStateSprites(
            Sprite("filled_normal"),
            Sprite("filled_hover"),
            Sprite("filled_normal")
        );
        this.ButtonStatesMap[LoadoutButtonState.Unavailable] = new ButtonStateSprites(
            Sprite("unavailable_normal"),
            Sprite("unavailable_hover"),
            Sprite("unavailable_normal")
        );
        this.ButtonStatesMap[LoadoutButtonState.Equipped] = new ButtonStateSprites(
            Sprite("equipped_normal"),
            Sprite("equipped_hover"),
            Sprite("equipped_normal")
        );
        LoadoutButton.ButtonStatesMap = this.ButtonStatesMap;
    }

    private LoadoutButton CreateButton(RectTransform parentRect, int idx)
    {
        var go = new GameObject($"loadout-button-{idx}");
        
        // positioning
        var transform = go.AddComponent<RectTransform>();
        transform.SetParent(parentRect);
        
        transform.anchorMin = new Vector2(0, 1);
        transform.anchorMax = new Vector2(0, 1);
        transform.anchoredPosition = new Vector2(idx * 6f, 0);
        transform.sizeDelta = new Vector2(5, 5);

        // click handling
        var handler = go.AddComponent<LoadoutButtonClickHandler>();
        handler.Callback = OnClick;
        handler.Index = idx;
        
        // button behaviour
        var loadoutButton = go.AddComponent<LoadoutButton>();

        return loadoutButton;
    }
    
    private void OnClick(LoadoutButton loadoutButton, int idx, PointerEventData.InputButton button)
    {
        switch (button)
        {
            case PointerEventData.InputButton.Left:
                this.callbacks.LoadSlot(idx);
                break;
            case PointerEventData.InputButton.Right:
                this.callbacks.SaveSlot(idx);
                break;
            case PointerEventData.InputButton.Middle:
                this.callbacks.ClearSlot(idx);
                break;
        }
    }
    
    public void UpdateLoadoutButtonsState(CharacterOverworld cow)
    {
        for (var idx = 0; idx < this.buttons.Length; idx++)
        {
            this.buttons[idx].SetState(GetButtonState(cow, idx));
        }
    }
    
    private LoadoutButtonState GetButtonState(CharacterOverworld cow, int idx)
    {
        bool isEnabled = GameLogic.Instance.IsSinglePlayer() && GameLogic.Instance.IsLocalMultiplayer() || cow.IsOwner || cow.m_WaitForRespawn;
        if (!isEnabled) return LoadoutButtonState.Disabled;
        
        if (this.loadouts.HasLoadoutAtSlot(cow.m_PlayerName, idx))
        {
            var containers = cow.m_PlayerInventory.m_Containers();
            var loadout = this.loadouts.Get(cow.m_PlayerName, idx);
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

    private static bool IsLoadoutCurrentlyEquipped(LoadoutDict dict, ContainersDict equippedContainers)
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
    
    private static bool HasAllItems(LoadoutDict dict, ContainersDict containers) => dict.All(pair => HasItem(containers, pair.Value));

    private static bool HasItem(Dictionary<PlayerInventory.ContainerID, ItemContainer> containers, FTK_itembase.ID id)
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