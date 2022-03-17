using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using AmadarePlugin.Common;
using AmadarePlugin.Extensions;
using AmadarePlugin.Features.Loadouts.Fitting;
using AmadarePlugin.Features.Loadouts.MaximizeStats;
using AmadarePlugin.Features.Loadouts.Sync;
using AmadarePlugin.Features.Loadouts.UI.Behaviors;
using AmadarePlugin.Options;
using AmadarePlugin.Resources;
using GridEditor;
using SimpleBind.Utilities;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace AmadarePlugin.Features.Loadouts.UI;

public partial class UILoadoutManager
{
    private static StringBuilder sharedStringBuilder = new();

    private readonly ILoadoutButtonsCallbacks callbacks;
    private readonly LoadoutRepository loadouts;
    private readonly CharacterShareTracker characterShareTracker;
    private readonly SyncService sync;
    private GameObject loadoutPanel = null;
    private LoadoutButton[] buttons = new LoadoutButton[LoadoutManager.SlotsCount];
    private Dictionary<LoadoutButtonState, ButtonStateSprites> ButtonStatesMap = new();

    public static int NewLineMod = 19;
    private KeyListener keyListener;
    private Toggle shareCheckbox;
    public bool IsShiftPressed => this.keyListener != null && this.keyListener.IsShiftPressed;

    public UILoadoutManager(ILoadoutButtonsCallbacks callbacks, LoadoutRepository loadouts,
        CharacterShareTracker characterShareTracker, SyncService sync)
    {
        this.callbacks = callbacks;
        this.loadouts = loadouts;
        this.characterShareTracker = characterShareTracker;
        this.sync = sync;
        
        On.uiPlayerInventory.ShowStats += OnShowStats;
        On.uiPlayerInventory.ShowCharacterInventory += OnShowCharacterInventory;
        On.uiPlayerInventory.OnClose += OnInventoryClose;
        On.CharacterOverworld.AddItemToBackpackRPC += OnAddItemToBackpackRPC;
        On.CharacterOverworld.RemoveItemRPC += OnRemoveItemRPC;
        On.CharacterOverworld.UnequipItemRPC += OnUnequipItemRPC;
        On.CharacterOverworld.EquipItemRPC += OnEquipItemRPC;
    }

    private void OnShowStats(On.uiPlayerInventory.orig_ShowStats orig, uiPlayerInventory self, CharacterOverworld _cow, bool _fromuicycler)
    {
        Utils.SafeInvoke(
            () => orig(self, _cow, _fromuicycler),
            () =>
            {
                if (this.loadoutPanel)
                {
                    Plugin.Log.LogInfo("Hiding loadout panel");
                    this.loadoutPanel.SetActive(false);
                    this.shareCheckbox.gameObject.SetActive(false);
                }
            });
    }

    private void OnEquipItemRPC(On.CharacterOverworld.orig_EquipItemRPC orig, CharacterOverworld self,
        FTK_itembase.ID item, bool _moveToBackpack)
    {
        orig(self, item, _moveToBackpack);
        Plugin.Log.LogInfo("OnEquipItemRPC");
        try
        {
            if (this.loadoutPanel && this.loadoutPanel.activeSelf && IsInventoryOpenFor(self))
                UpdateMaximizeStatsButtons(self);
        }
        catch (Exception e)
        {
            Plugin.Log.LogError(e);
        }
    }

    private void OnUnequipItemRPC(On.CharacterOverworld.orig_UnequipItemRPC orig, CharacterOverworld self,
        FTK_itembase.ID item, bool _moveToBackpack)
    {
        orig(self, item, _moveToBackpack);
        try
        {
            Plugin.Log.LogInfo("UnequipItemRPC");
            if (this.loadoutPanel && this.loadoutPanel.activeSelf && IsInventoryOpenFor(self))
            {
                UpdateLoadoutButtonsState(self);
            }
        }
        catch (Exception e)
        {
            Plugin.Log.LogError(e);
        }
    }

    private void OnRemoveItemRPC(On.CharacterOverworld.orig_RemoveItemRPC orig, CharacterOverworld self,
        PlayerInventory.ContainerID id, FTK_itembase.ID item, bool consumed)
    {
        orig(self, id, item, consumed);
        try
        {
            Plugin.Log.LogInfo("RemoveItemRPC");
            if (this.loadoutPanel && this.loadoutPanel.activeSelf && IsInventoryOpenFor(self))
            {
                UpdateLoadoutButtonsState(self);
            }
        }
        catch (Exception e)
        {
            Plugin.Log.LogError(e);
        }
    }

    private void OnAddItemToBackpackRPC(On.CharacterOverworld.orig_AddItemToBackpackRPC orig, CharacterOverworld self,
        FTK_itembase.ID item, bool hud)
    {
        orig(self, item, hud);
        try
        {
            Plugin.Log.LogInfo("AddItemToBackpackRPC");
            if (this.loadoutPanel && this.loadoutPanel.activeSelf && IsInventoryOpenFor(self))
            {
                UpdateLoadoutButtonsState(self);
            }
        }
        catch (Exception e)
        {
            Plugin.Log.LogError(e);
        }
    }

    private void OnInventoryClose(On.uiPlayerInventory.orig_OnClose orig, uiPlayerInventory self)
    {
        orig(self);
        try
        {
            if (this.loadoutPanel) this.loadoutPanel.SetActive(false);
        }
        catch (Exception e)
        {
            Plugin.Log.LogError(e);
        }
    }

    private void OnShowCharacterInventory(On.uiPlayerInventory.orig_ShowCharacterInventory orig, uiPlayerInventory self,
        CharacterOverworld cow, bool cycler)
    {
        orig(self, cow, cycler);
        if (!this.loadoutPanel || this.loadoutPanel == null)
        {
            RuntimeResources.AssertInited();
            var rect = (RectTransform)self.gameObject.transform.Find("InventoryBackground").transform;
            InitLoadoutPanel(rect);
            UpdateLoadoutButtonsState(cow);
        }
        else
        {
            this.loadoutPanel.SetActive(true);
        }

        if (OptionsManager.AlwaysShare)
        {
            if (this.shareCheckbox) 
            {
                this.shareCheckbox.gameObject.SetActive(false);
            }
        }
        else 
        {
            if (!this.shareCheckbox)
            {
                this.shareCheckbox = CreateShareCheckbox((RectTransform)this.loadoutPanel.transform);
            }

            this.shareCheckbox.gameObject.SetActive(true);
            this.shareCheckbox.interactable = FtkHelpers.IsInventoryOwner;
        }

        UpdateLoadoutButtonsState(cow);
    }

    private bool IsInventoryOpenFor(CharacterOverworld cow)
    {
        return FtkHelpers.InventoryOwner.m_FTKPlayerID.PhotonID == cow.m_FTKPlayerID.PhotonID;
    }

    private void InitLoadoutPanel(RectTransform parentRect)
    {
        InitButtonStatesMap();

        var panel = new GameObject("loadout-panel").SetUILayer();
        this.loadoutPanel = panel;
        var transform = panel.AddComponent<RectTransform>();
        transform.ScaleResolutionBased()
            .SetParent(parentRect);

        transform.anchorMin = new Vector2(0, 1);
        transform.anchorMax = new Vector2(0, 1);
        transform.anchoredPosition = new Vector2(21, -415);
        transform.sizeDelta = new Vector2(0, 5);
        transform.pivot = Vector2.zero;

        var layout = panel.AddComponent<HorizontalLayoutGroup>();
        layout.spacing = 1;
        layout.childControlWidth = false;
        layout.childControlHeight = true;

        var sizeFitter = panel.AddComponent<ContentSizeFitter>();
        sizeFitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;

        this.keyListener = panel.AddComponent<KeyListener>();
        this.keyListener.Init(OnKeyPressChanged);

        // loadout buttons
        for (var i = 0; i < LoadoutManager.SlotsCount; i++)
        {
            this.buttons[i] = CreateButton(transform, i);
        }

        if (!OptionsManager.AlwaysShare)
        {
            this.shareCheckbox = CreateShareCheckbox(parentRect);
        }

        InitMaximizeStatButtons(transform);
    }

    private Toggle CreateShareCheckbox(RectTransform parent)
    {
        var shareCheckbox = UiFactory.CreateCheckBox(
            name: "Share", 
            tooltipTitle: "Share",
            tooltipDetails: "If enabled, this character's backpack content may be transferred automatically by loadouts.", 
            value: this.characterShareTracker.Get(FtkHelpers.InventoryCharacterName)
        );
        var transform = (RectTransform)shareCheckbox.transform;
        
        transform.SetParent(parent);
        transform.Scale(.8f);
        transform.anchorMin = Vector2.zero;
        transform.anchorMax = Vector2.one;
        transform.anchoredPosition = new Vector2(-100, -315);
        transform.sizeDelta = Vector2.zero;
        transform.pivot = new Vector2(1, .5f);
        
        var sizeFitter = shareCheckbox.AddComponent<ContentSizeFitter>();
        sizeFitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;

        shareCheckbox.GetComponent<uiToolTipGeneral>().m_ToolTipOffset = new Vector2(0, -100);

        var toggle = shareCheckbox.GetComponent<Toggle>();
        toggle.onValueChanged.AddListener(OnSharedValueChanged);
        toggle.interactable = FtkHelpers.IsInventoryOwner;

        return toggle;
    }

    private void OnSharedValueChanged(bool isShared)
    {
        Plugin.Log.LogInfo($"On shared value changed {FtkHelpers.InventoryCharacterName} {isShared}");
        this.characterShareTracker.Set(FtkHelpers.InventoryCharacterName, isShared);
        this.sync.SyncLoadouts();
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
        var go = new GameObject($"loadout-button-{idx}").SetUILayer();

        // positioning
        var transform = go.AddComponent<RectTransform>();
        transform
            .ScaleResolutionBased()
            .SetParent(parentRect);

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
        handler.Button = loadoutButton;

        return loadoutButton;
    }

    private void OnExpandedChanged(bool state)
    {
        UpdateLoadoutButtonsState(FtkHelpers.InventoryOwner);
    }

    private void OnBlur(LoadoutButton arg1, int arg2)
    {
        if (OptionsManager.TestFit)
        {
            LoadoutFittingHelper.ResetDisplay(FtkHelpers.InventoryOwner);
        }
    }

    private void OnHover(LoadoutButton btn, int idx)
    {
        if (OptionsManager.TestFit)
        {
            var cow = FtkHelpers.InventoryOwner;
            var loadout = this.loadouts.Get(cow.GetCowUniqueName(), idx);
            if (loadout.Count > 0)
            {
                // TODO: use cached stat dummy
                LoadoutFittingHelper.TestFit(cow, loadout);
            }
        }
    }

    private void OnClick(LoadoutButton loadoutButton, int idx, PointerEventData.InputButton button)
    {
        if (!FtkHelpers.IsInventoryOwner)
        {
            Plugin.Log.LogDebug("Loadout operation prevented - wrong owner");
            return;
        }
        
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
            UpdateTooltipOffset(button.tooltip);
            var newlines = button.tooltip.m_DetailInfo.Count(c => c == '\n');
            var verticalShift = newlines <= 1
                ? 60
                : (90 + newlines * NewLineMod) * TransformExtensions.ResolutionFactorY;
            button.tooltip.m_ToolTipOffset = new Vector2(0, -verticalShift);
        }

        if (OptionsManager.MaximizeStatButtons)
        {
            this.localStatDummys = InventoryCalculatorHelper.GetMaximizeStatLoadout(cow, new List<CharacterOverworld>(0));
            this.distributedMaximizedStatDummys = InventoryCalculatorHelper.GetMaximizeStatLoadout(cow, cow
                .GetLinkedPlayers(false)
                .Where(lp => this.characterShareTracker.Get(lp.m_CharacterStats.m_CharacterName))
                .ToList()
            );
        }

        UpdateButtonDisplay(cow);
    }

    private void UpdateTooltipOffset(uiToolTipGeneral tooltip)
    {
        var newlines = tooltip.m_DetailInfo.Count(c => c == '\n');
        var verticalShift = newlines <= 1
            ? 60
            : (90 + newlines * NewLineMod) * TransformExtensions.ResolutionFactorY;
        tooltip.m_ToolTipOffset = new Vector2(0, -verticalShift);
    }

    private void UpdateButtonDisplay(CharacterOverworld cow)
    {
        UpdateLoadoutButtons();
        UpdateMaximizeStatsButtons(cow);
        UpdateShareCheckbox();
    }

    private void UpdateShareCheckbox()
    {
        if (this.shareCheckbox != null && this.shareCheckbox.gameObject != null)
        {
            this.shareCheckbox.isOn = this.characterShareTracker.Get(FtkHelpers.InventoryCharacterName);
        }
    }

    private void UpdateLoadoutButtons()
    {
        if (OptionsManager.HideExtraSlots)
        {
            if (!this.buttons.Any() || this.buttons.Any(b => b == null))
            {
                var rect = (RectTransform)GameObject.Find("InventoryBackground").transform;
                Plugin.Log.LogError("Loadout buttons were empty, recreating them...");
                InitMaximizeStatButtons(rect);
                return;
            }

            var lastVisibleIdx = 0;
            if (this.buttons.Last().State.IsOccupied())
            {
                // if last button is occupied, display all buttons
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
    }

    private string GetButtonTooltipText(CharacterOverworld cow, LoadoutButtonState loadoutButtonState, int idx)
    {
        if (loadoutButtonState == LoadoutButtonState.Empty)
        {
            return "<color=#444444>LMB or RMB to save</color>";
        }

        sharedStringBuilder.Clear();

        var dictionary = this.loadouts.Get(cow.GetCowUniqueName(), idx);
        if (loadoutButtonState == LoadoutButtonState.Equipped)
        {
            sharedStringBuilder.AppendLine("<color=#add8e6ff>(active)</color>");
        }

        AppendItemListing(cow, dictionary);

        if (FtkHelpers.IsInventoryOwner)
        {
            sharedStringBuilder.AppendLine()
                .Append("<color=#444444>LMB to equip; MMB to clear; RMB to override</color>");
        }

        return sharedStringBuilder.ToString();
    }

    
    private string GetMaximizeLoadoutTooltipText(CharacterOverworld cow, FittingCharacterStats stats)
    {
        sharedStringBuilder.Clear();
        AppendItemListing(cow, stats.Loadout, stats.OwnershipMap);
        if (FtkHelpers.IsInventoryOwner)
        {
            sharedStringBuilder.AppendLine()
                .Append("<color=#444444>LMB to equip; Hold Shift to see shared</color>");
        }

        return sharedStringBuilder.ToString();
    }

    private static void AppendItemListing(CharacterOverworld cow, Dictionary<PlayerInventory.ContainerID, FTK_itembase.ID> dictionary, Dictionary<FTK_itembase.ID, CharacterOverworld> ownershipMap = null)
    {
        foreach (var pair in dictionary)
        {
            var containerId = pair.Key;
            var itemId = pair.Value;
            var itemName = itemId.GetLocalizedName();
            var playerInv = cow.m_PlayerInventory;
            var itemMissing = !LoadoutStateEvaluator.HasItem(playerInv.m_Containers, itemId);
            if (itemMissing)
            {
                if (ownershipMap != null && ownershipMap.TryGetValue(itemId, out var owner) && owner != cow)
                {
                    sharedStringBuilder
                        .Append(itemName)
                        .Append(" (")
                        .Append(owner.m_CharacterStats.m_CharacterName)
                        .AppendLine(")");
                }
                else
                {
                    sharedStringBuilder
                        .Append("<color=red>") // item is missing color
                        .Append(itemName)
                        .AppendLine("</color>");
                }
            }
            else if (playerInv.GetItemCount(containerId, itemId) > 0)
            {
                sharedStringBuilder
                    .Append("<color=#add8e6ff>") // equipped color
                    .Append(itemName)
                    .AppendLine("</color>");
            }
            else
            {
                sharedStringBuilder.AppendLine(itemName);
            }
        }
    }
}