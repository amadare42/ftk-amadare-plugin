using System.Collections.Generic;
using System.Linq;
using System.Text;
using AmadarePlugin.InventoryPresets.UI.Behaviors;
using AmadarePlugin.Options;
using AmadarePlugin.Resources;
using Google2u;
using SimpleBind.Utilities;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using LoadoutDict = System.Collections.Generic.Dictionary<PlayerInventory.ContainerID, GridEditor.FTK_itembase.ID>;
using ContainersDict = System.Collections.Generic.Dictionary<PlayerInventory.ContainerID, ItemContainer>;

namespace AmadarePlugin.InventoryPresets.UI;

public class UiInventoryLoadoutManager
{
    private static StringBuilder sharedStringBuilder = new();
    private readonly ILoadoutButtonsCallbacks callbacks;
    private readonly LoadoutRepository loadouts;
    private GameObject loadoutPanel = null;
    private LoadoutButton[] buttons = new LoadoutButton[LoadoutManager.SlotsCount];
    private Dictionary<LoadoutButtonState, ButtonStateSprites> ButtonStatesMap = new();
    private bool isExpanded = false;

    public static int NewLineMod = 19;
    private ShowMoreButton showMoreBtn;

    private Dictionary<PlayerInventory.ContainerID, Texture2D> ContainerIcons = new();

    public UiInventoryLoadoutManager(ILoadoutButtonsCallbacks callbacks, LoadoutRepository loadouts)
    {
        this.callbacks = callbacks;
        this.loadouts = loadouts;
        On.uiPlayerInventory.ShowCharacterInventory += (orig, self, cow, cycler) =>
        {
            orig(self, cow, cycler);
            var rect = (RectTransform)self.gameObject.transform.Find("InventoryBackground").transform;
            if (!this.loadoutPanel || this.loadoutPanel == null)
            {
                RuntimeResources.Init();
                InitLoadoutButtons(rect);
                InitContainerIcons();
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
            if (this.loadoutPanel)
                this.loadoutPanel.SetActive(false);
        };
        On.uiItemMenu.Give += (orig, self, b) =>
        {
            orig(self, b);
            if (this.loadoutPanel && this.loadoutPanel.activeSelf)
            {
                UpdateLoadoutButtonsState(FTKUI.Instance.m_PlayerInventory.m_InventoryOwner);
            }
        };
        On.CharacterOverworld.AddItemToBackpackRPC += (orig, self, item, hud) =>
        {
            orig(self, item, hud);
            Plugin.Log.LogInfo("AddItemToBackpackRPC");
            if (this.loadoutPanel && this.loadoutPanel.activeSelf)
            {
                UpdateLoadoutButtonsState(FTKUI.Instance.m_PlayerInventory.m_InventoryOwner);
            }
        };
        On.CharacterOverworld.RemoveItemRPC += (orig, self, id, item, consumed) =>
        {
            orig(self, id, item, consumed);
            Plugin.Log.LogInfo("RemoveItemRPC");
            if (this.loadoutPanel && this.loadoutPanel.activeSelf)
            {
                UpdateLoadoutButtonsState(FTKUI.Instance.m_PlayerInventory.m_InventoryOwner);
            }
        };
        On.CharacterOverworld.UnequipItemRPC += (orig, self, item, _moveToBackpack) =>
        {
            orig(self, item, _moveToBackpack);
            Plugin.Log.LogInfo("UnequipItemRPC");
            if (this.loadoutPanel && this.loadoutPanel.activeSelf)
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

        // loadout buttons
        for (var i = 0; i < LoadoutManager.SlotsCount; i++)
        {
            this.buttons[i] = CreateButton(transform, i);
        }
    }

    private void InitButtonStatesMap()
    {
        Sprite Sprite(string name) => RuntimeResources.LoadButton(name);

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

    private void InitContainerIcons()
    {
        Plugin.Log.LogInfo("Fetching container icons...");
        
        var images = UnityEngine.GameObject.Find("EquipRoot")
            .GetComponentsInChildren<UnityEngine.UI.Image>()
            .Where(i => i.name.StartsWith("ItemTargetBG"))
            .ToArray();

        if (images.Length != LoadoutManager.EquipableContainers.Length)
        {
            Plugin.Log.LogWarning($"Images count missmatch! {images.Length}");
            return;
        }
        
        for (var i = 0; i < LoadoutManager.EquipableContainers.Length; i++)
        {
            ContainerIcons[LoadoutManager.EquipableContainers[i]] = images[i].sprite.texture;
        }

        Plugin.Log.LogInfo("Fetching container icons done...");
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
        transform.SetHeight(5);

        // click handling
        var handler = go.AddComponent<LoadoutButtonPointerHandler>();
        handler.Callback = OnClick;
        handler.HoverCallback = OnHover;
        handler.BlurCallback = OnBlur;
        handler.Index = idx;
        
        // tooltip
        var tooltip = go.AddComponent<uiToolTipGeneral>();
        tooltip.m_IsFollowHoriz = true;
        tooltip.m_ReturnRawInfo = true;
        tooltip.m_Info = "Loadout " + (idx + 1);
        tooltip.m_ToolTipOffset = new Vector2(0, -100);
        
        // button behaviour
        var loadoutButton = go.AddComponent<LoadoutButton>();
        loadoutButton.tooltip = tooltip;
        
        return loadoutButton;
    }
    
    private void CreateMoreButton(RectTransform parentRect)
    {
        var go = new GameObject($"loadout-button-more");
        
        // positioning
        var transform = go.AddComponent<RectTransform>();
        transform.SetParent(parentRect);
        
        transform.anchorMin = new Vector2(0, 1);
        transform.anchorMax = new Vector2(0, 1);
        transform.sizeDelta = new Vector2(5, 5);
        transform.anchoredPosition = new Vector2(32f, 0);

        this.showMoreBtn = go.AddComponent<ShowMoreButton>();
        this.showMoreBtn.OnExpandedChanged += OnExpandedChanged;
    }

    private void OnExpandedChanged(bool state)
    {
        UpdateLoadoutButtonsState(FTKUI.Instance.m_PlayerInventory.m_InventoryOwner);
    }

    private void OnBlur(LoadoutButton arg1, int arg2)
    {
        if (OptionsManager.TestFit)
        {
            var cow = FTKUI.Instance.m_PlayerInventory.m_InventoryOwner;
            UIEqipementFittingHelper.Reset(cow);
        }
    }

    private void OnHover(LoadoutButton btn, int idx)
    {
        if (OptionsManager.TestFit)
        {
            var cow = FTKUI.Instance.m_PlayerInventory.m_InventoryOwner;
            var loadout = this.loadouts.Get(cow.m_PlayerName, idx);
            if (loadout.Count > 0)
            {
                UIEqipementFittingHelper.TestFit(cow, loadout);
            }
        }
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
            var button = this.buttons[idx];
            
            var state = LoadoutStateEvaluator.GetButtonState(cow, this.loadouts, idx);
            button.SetState(state);
            button.tooltip.m_DetailInfo = GetButtonTooltipText(cow, state, idx);
            // it ain't stupid if it works!
            button.tooltip.m_ToolTipOffset = new Vector2(0, - 90 - button.tooltip.m_DetailInfo.Count(c => c == '\n') * NewLineMod);
        }

        // if (OptionsManager.HideExtraSlots)
        // {
        //     for (var idx = this.buttons.Length - 1; idx > 0; idx--)
        //     {
        //         var button = this.buttons[idx];
        //         if (button.State is LoadoutButtonState.Empty or LoadoutButtonState.Disabled)
        //         {
        //             button.gameObject.SetActive(false);
        //         }
        //     }
        // }
    }

    private string GetButtonTooltipText(CharacterOverworld cow, LoadoutButtonState loadoutButtonState, int idx)
    {
        // clear for .net 3.5
        sharedStringBuilder.Length = 0;

        var dictionary = this.loadouts.Get(cow.m_PlayerName, idx);
        if (loadoutButtonState == LoadoutButtonState.Equipped)
        {
            sharedStringBuilder.AppendLine("<color=#add8e6ff>(active)</color>");
        }

        foreach (var pair in dictionary)
        {
            var itemName = FTKHub.Localized<TextItems>("STR_" + pair.Value);
            var itemMissing = !LoadoutStateEvaluator.HasItem(cow.m_PlayerInventory.m_Containers, pair.Value);
            if (itemMissing)
            {
                sharedStringBuilder
                    .Append("<color=red>")
                    .Append(itemName)
                    .AppendLine("</color>");
            }
            else
            {
                sharedStringBuilder.AppendLine(itemName);
            }
        }

        sharedStringBuilder.AppendLine()
            .Append("<color=#444444>LMB to equip; MMB to clear; RMB to save</color>");

        return sharedStringBuilder.ToString();
    }
}