﻿using System;
using AmadarePlugin.Extensions;
using AmadarePlugin.Options;
using AmadarePlugin.Resources;
using GridEditor;
using UnityEngine;
using UnityEngine.UI;

namespace AmadarePlugin;

public class ItemCardPrice
{
    private static string PanelName = "price-panel";
    private static string LabelName = "price-label";

    private Text cachedLabel = null;
    private GameObject cachedPanel = null;
    
    public ItemCardPrice()
    {
        On.uiInventoryItemDisplay.Show_ID_Transform_CharacterOverworld_Mode_string_Texture_bool_int_uiLoreCard_bool_bool += 
            OnItemPopover;
    }

    private GameObject OnItemPopover(On.uiInventoryItemDisplay.orig_Show_ID_Transform_CharacterOverworld_Mode_string_Texture_bool_int_uiLoreCard_bool_bool orig, uiInventoryItemDisplay self, FTK_itembase.ID _itemid, Transform _lastowner, CharacterOverworld _cow, uiItemDetail.Mode _mode, string _cameraid, Texture _textureoverride, bool _isinventory, int _amount, uiLoreCard _lorecard, bool _showequip, bool _forcefrontside)
    {
        GameObject r = null;
        try
        {
            r = orig(self, _itemid, _lastowner, _cow, _mode, _cameraid, _textureoverride, _isinventory, _amount,
                _lorecard, _showequip, _forcefrontside);

            if (OptionsManager.ShowBasePrice)
            {
                UpdatePrice(self, _itemid);
            }
        }
        catch (Exception ex)
        {
            Plugin.Log.LogError(ex);
        }

        return r;
    }

    private void UpdatePrice(uiInventoryItemDisplay itemDisplay, FTK_itembase.ID itemId)
    {
        var itembase = FTK_itembase.GetItemBase(itemId);
        var txt = GetPriceLabel((RectTransform)itemDisplay.gameObject.transform);
        var price = CalculatePrice(itembase);
        if (price == 0)
        {
            this.cachedPanel.SetActive(false);
        }
        else
        {
            this.cachedPanel.SetActive(true);
            txt.text = price.ToString();
        }
    }

    private int CalculatePrice(FTK_itembase itembase)
    {
        return FTKUtil.RoundToInt(FTKUtil.Price(itembase._goldValue, 1, itembase.m_PriceScale) *
                                  GameFlow.Instance.GameDif.m_ItemSellValue);
    }

    private Text GetPriceLabel(RectTransform parent)
    {
        if (this.cachedLabel != null && this.cachedLabel.gameObject != null)
        {
            return this.cachedLabel;
        }
        
        Plugin.Log.LogInfo("Creating new price label..");

        // Creating PANEL
        var panel = new GameObject(PanelName);
        var transform = panel.AddComponent<RectTransform>();
        
        transform.SetParent(parent);
        transform.anchorMin = new Vector2(1, 0);
        transform.anchorMax = new Vector2(1, 0);
        transform.pivot = new Vector2(1, 0);
        transform.anchoredPosition = new Vector2(-12, 12);
        // for some reason it became scaled to .5, just compensating for it
        transform.Scale1();

        CreateCoinsIcon(transform);
        var txt = CreatePriceLabel(transform);
        
        Plugin.Log.LogInfo("Creating new price label DONE!");
        this.cachedPanel = panel;
        return txt;
    }

    private void CreateCoinsIcon(RectTransform parent)
    {
        // Creating ICON
        var obj = new GameObject("coins-icon");
        var transform = obj.AddComponent<RectTransform>();
        transform.SetParent(parent);
        
        transform.anchorMin = new Vector2(1, 0);
        transform.anchorMax = new Vector2(1, 0);
        transform.pivot = new Vector2(1, 0);
        transform.sizeDelta = new Vector2(32, 32);
        transform.anchoredPosition = new Vector2(0, 5);
        transform.Scale1();
        
        var img = obj.AddComponent<Image>();
        img.sprite = RuntimeResources.Get<Sprite>("coins");
        img.color = new Color(0.7721f, 0.7342f, 0.6528f, 0.466f);
    }

    private Text CreatePriceLabel(RectTransform parent)
    {
        // Creating LABEL
        var obj = new GameObject(LabelName);
        var transform = obj.AddComponent<RectTransform>();
        transform.SetParent(parent);
        
        transform.anchoredPosition = new Vector2(-28, 2);
        transform.anchorMin = new Vector2(.5f, 0);
        transform.anchorMax = new Vector2(1, 0);
        transform.pivot = new Vector2(1, 0);
        transform.Scale1();
        
        var txt = obj.AddComponent<Text>();
        txt.font = RuntimeResources.Get<Font>("Kingthings Petrock");
        txt.fontSize = 32;
        txt.color = VisualParams.Instance.m_ColorTints.m_Gold;
        txt.alignment = TextAnchor.LowerRight;

        this.cachedLabel = txt;

        return txt;
    }
}