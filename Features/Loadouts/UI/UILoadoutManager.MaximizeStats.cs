using System;
using System.Collections.Generic;
using System.Linq;
using AmadarePlugin.Common;
using AmadarePlugin.Extensions;
using AmadarePlugin.Features.Loadouts.Fitting;
using AmadarePlugin.Features.Loadouts.MaximizeStats;
using AmadarePlugin.Features.Loadouts.UI.Behaviors;
using AmadarePlugin.Options;
using AmadarePlugin.Resources;
using SimpleBind.Utilities;
using UnityEngine;
using UnityEngine.UI;

namespace AmadarePlugin.Features.Loadouts.UI;

public partial class UILoadoutManager
{
    private readonly List<MaximizeStatButton> maximizeStatButtons = new();
    
    private Dictionary<StatType,FittingCharacterStats> localStatDummys;
    private Dictionary<StatType,FittingCharacterStats> distributedMaximizedStatDummys;
    private Dictionary<StatType, FittingCharacterStats> maximizedStatDummys =>
        this.IsShiftPressed ? this.distributedMaximizedStatDummys : this.localStatDummys;

    private readonly MaximizeStatService maximizeStatService;
    private ShowMoreButton showMoreBtn;
    
    
    private void CreateMoreButton(RectTransform parentRect)
    {
        var go = new GameObject($"loadout-button-more").SetUILayer();
        
        // positioning
        var transform = go.AddComponent<RectTransform>();
        transform
            .ScaleResolutionBased()
            .SetParent(parentRect);
        
        transform.anchorMin = new Vector2(0, 1);
        transform.anchorMax = new Vector2(0, 1);
        transform.pivot = new Vector2(.5f, .5f);
        transform.sizeDelta = new Vector2(5, 5);

        var layoutElement = go.AddComponent<LayoutElement>();
        layoutElement.preferredWidth = 5;
        layoutElement.preferredHeight = 5;

        this.showMoreBtn = go.AddComponent<ShowMoreButton>();
        this.showMoreBtn.OnExpandedChanged += OnExpandedChanged;
    }


    private MaximizeStatButton CreateMaximizeStatButton(RectTransform parentRect, StatType stat)
    {
        // TODO: button pooling
        var go = new GameObject($"maximize-skill-{stat:G}").SetUILayer();
        // positioning
        var transform = go.AddComponent<RectTransform>();
        transform
            .ScaleResolutionBased()
            .SetParent(parentRect);
        
        transform.anchorMin = new Vector2(0, 1);
        transform.anchorMax = new Vector2(0, 1);
        transform.pivot = new Vector2(.5f, .5f);
        transform.sizeDelta = new Vector2(5, 5);
        transform.SetHeight(5);

        var layoutElement = go.AddComponent<LayoutElement>();
        layoutElement.preferredWidth = 5;
        layoutElement.preferredHeight = 5;

        var tooltip = go.AddComponent<uiToolTipGeneral>();
        tooltip.m_IsFollowHoriz = true;
        tooltip.m_ReturnRawInfo = true;
        tooltip.m_Info = $"Maximize {stat:G}";
        tooltip.m_ToolTipOffset = new Vector2(0, -40);

        var button = go.AddComponent<MaximizeStatButton>();
        button.Init(
            RuntimeResources.Get<Sprite>(MaximizeStatService.StatToIconMap[stat]),
            tooltip,
            stat,
            OnMaximizedLoadoutClick,
            OnMaximizedLoadoutHover,
            OnMaximizedLoadoutBlur
        );

        return button;
    }
    private void OnMaximizedLoadoutBlur(StatType stat)
    {
        if (OptionsManager.TestFit)
        {
            var cow = FTKUI.Instance.m_PlayerInventory.m_InventoryOwner;
            LoadoutFittingHelper.ResetDisplay(cow);
        }
    }

    private void OnMaximizedLoadoutHover(StatType stat)
    {
        if (OptionsManager.TestFit && this.maximizedStatDummys.TryGetValue(stat, out var dummy))
        {
            LoadoutFittingHelper.TestFit(dummy);
        }
    }
    private void OnMaximizedLoadoutClick(StatType stat)
    {
        if (this.maximizedStatDummys.TryGetValue(stat, out var dummy) && dummy.Loadout != null)
        {
            this.callbacks.LoadLoadout(FtkHelpers.InventoryOwner, dummy.Loadout, this.IsShiftPressed);
        }
    }
    
    private void InitMaximizeStatButtons(RectTransform transform)
    {
        if (!OptionsManager.MaximizeStatButtons) return;
        
        CreateMoreButton(transform);
        
        this.maximizeStatButtons.Clear();
        // maximize stat buttons
        for (var i = 0; i < (int)StatType.COUNT; i++)
        {
            var statType = (StatType)i;
            this.maximizeStatButtons.Add(CreateMaximizeStatButton(transform, statType));
        }
    }

    private void UpdateMaximizeStatsButtons(CharacterOverworld cow)
    {
        if (!this.maximizeStatButtons.Any() || this.maximizeStatButtons.Any(x => x == null))
        {
            Plugin.Log.LogError("Maximize stat buttons were empty");
            return;
        }

        foreach (var button in this.maximizeStatButtons)
        {
            var increase = GetStatMod(button.stat, cow, out var stats);
            var visible = this.showMoreBtn.IsExpanded && increase > 0;
            button.gameObject.SetActive(visible);
            if (visible)
            {
                button.tooltip.m_Info = $"Maximize {button.stat:G} (<color=green>{increase}</color>)";
                button.tooltip.m_DetailInfo = GetMaximizeLoadoutTooltipText(cow, stats);
            }
        }
    }

    private int GetStatMod(StatType statType, CharacterOverworld cow, out FittingCharacterStats stats)
    {
        if (this.maximizedStatDummys.TryGetValue(statType, out stats))
        {
            var potentialValue = Mathf.Clamp(MaximizeStatService.GetStatValue(statType, stats), 0, 0.95f);
            var currentValue = MaximizeStatService.GetStatValue(statType, cow.m_CharacterStats);
            if (potentialValue > currentValue)
            {
                return (int)Math.Round(potentialValue * 100, 0);
            }
        }

        return 0;
    }

    private void OnKeyPressChanged(KeyListener listener)
    {
        UpdateMaximizeStatsButtons(FtkHelpers.InventoryOwner);
    }
}