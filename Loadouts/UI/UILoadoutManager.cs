using System.Collections.Generic;
using System.Linq;
using System.Text;
using AmadarePlugin.Extensions;
using AmadarePlugin.Loadouts.Fitting;
using AmadarePlugin.Loadouts.UI.Behaviors;
using AmadarePlugin.Options;
using AmadarePlugin.Resources;
using Google2u;
using SimpleBind.Utilities;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace AmadarePlugin.Loadouts.UI;

public partial class UILoadoutManager
{
    private static StringBuilder sharedStringBuilder = new();
    
    private readonly ILoadoutButtonsCallbacks callbacks;
    private readonly LoadoutRepository loadouts;
    private GameObject loadoutPanel = null;
    private LoadoutButton[] buttons = new LoadoutButton[LoadoutManager.SlotsCount];
    private Dictionary<LoadoutButtonState, ButtonStateSprites> ButtonStatesMap = new();

    public static int NewLineMod = 19;

    public UILoadoutManager(ILoadoutButtonsCallbacks callbacks, LoadoutRepository loadouts)
    {
        this.callbacks = callbacks;
        this.loadouts = loadouts;
        this.maximizeStatService = new MaximizeStatService();
        On.uiPlayerInventory.ShowCharacterInventory += (orig, self, cow, cycler) =>
        {
            orig(self, cow, cycler);
            var rect = (RectTransform)self.gameObject.transform.Find("InventoryBackground").transform;
            if (!this.loadoutPanel || this.loadoutPanel == null)
            {
                RuntimeResources.Init();
                InitLoadoutPanel(rect);
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
    
    private void InitLoadoutPanel(RectTransform parentRect)
    {
        InitButtonStatesMap();
        
        var panel = new GameObject("loadout-panel");
        this.loadoutPanel = panel;
        var transform = panel.AddComponent<RectTransform>();
        transform.SetParent(parentRect);
        
        transform.anchorMin = new Vector2(0, 1);
        transform.anchorMax =  new Vector2(0, 1);
        transform.anchoredPosition = new Vector2(21, -415);
        transform.sizeDelta = new Vector2(0, 5);
        transform.pivot = Vector2.zero;

        var layout = panel.AddComponent<HorizontalLayoutGroup>();
        layout.spacing = 1;
        layout.childControlWidth = false;
        layout.childControlHeight = true;

        var sizeFitter = panel.AddComponent<ContentSizeFitter>();
        sizeFitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;

        // loadout buttons
        for (var i = 0; i < LoadoutManager.SlotsCount; i++)
        {
            this.buttons[i] = CreateButton(transform, i);
        }

        InitMaximizeStatButtons(transform);
    }

    private void InitButtonStatesMap()
    {
        Sprite Sprite(string name) => RuntimeResources.Get<Sprite>(name);

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
        transform.pivot = new Vector2(.5f, .5f);
        transform.sizeDelta = new Vector2(5, 5);
        transform.SetHeight(5);

        var layoutElement = go.AddComponent<LayoutElement>();
        layoutElement.preferredWidth = 5;
        layoutElement.preferredHeight = 5;

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
    
    private void OnExpandedChanged(bool state)
    {
        UpdateLoadoutButtonsState(FTKUI.Instance.m_PlayerInventory.m_InventoryOwner);
    }

    private void OnBlur(LoadoutButton arg1, int arg2)
    {
        if (OptionsManager.TestFit)
        {
            var cow = FTKUI.Instance.m_PlayerInventory.m_InventoryOwner;
            LoadoutFittingHelper.ResetDisplay(cow);
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
                // TODO: use cached stat dummy
                LoadoutFittingHelper.TestFit(cow, loadout);
            }
        }
    }

    private void OnClick(LoadoutButton loadoutButton, int idx, PointerEventData.InputButton button)
    {
        switch (button)
        {
            case PointerEventData.InputButton.Left:
                if (loadoutButton.State == LoadoutButtonState.Empty)
                {
                    this.callbacks.SaveSlot(idx);
                }
                else
                {
                    this.callbacks.LoadSlot(idx);
                }

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

        if (OptionsManager.OptimizeStatButtons)
        {
            this.maximizedStatDummys = this.maximizeStatService.GetOptimizedLoadouts(cow);
        }

        UpdateButtonDisplay();
    }

    private void UpdateButtonDisplay()
    {
        if (OptionsManager.HideExtraSlots)
        {
            var lastVisibleIdx = 0;
            if (this.buttons.Last().State.IsOccupied())
            {
                lastVisibleIdx = this.buttons.Length - 1;
            }
            else
            {
                for (var idx = this.buttons.Length - 1; idx >= 0; idx--)
                {
                    var button = this.buttons[idx];
                    if (!button.State.IsOccupied())
                    {
                        if (idx > 1 && this.buttons[idx - 1].State.IsOccupied())
                        {
                            // make sure that there is extra available slot
                            lastVisibleIdx = idx;
                            break;
                        }

                        lastVisibleIdx = idx;
                    }
                }
            }

            for (var i = 0; i < this.buttons.Length; i++)
            {
                this.buttons[i].gameObject.SetActive(i <= lastVisibleIdx);
            }
        }

        foreach (var button in this.maximizeStatButtons)
        {
            button.gameObject.SetActive(this.showMoreBtn.IsExpanded);
        }
        
    }

    private string GetButtonTooltipText(CharacterOverworld cow, LoadoutButtonState loadoutButtonState, int idx)
    {
        if (loadoutButtonState == LoadoutButtonState.Empty)
        {
            return "<color=#444444>LMB or RMB to save</color>";
        }
        
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