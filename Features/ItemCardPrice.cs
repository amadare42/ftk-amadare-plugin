using System;
using AmadarePlugin.Common;
using AmadarePlugin.Options;
using AmadarePlugin.Resources;
using FTKAPI.Utils;
using GridEditor;
using UnityEngine;
using UnityEngine.UI;

namespace AmadarePlugin.Features;

public class ItemCardPrice
{
    private static string PanelName = "price-panel";
    private static string LabelName = "price-label";

    private static Color CanSellColor = new Color(0.3255f, 0.6824f, 0.4756f);
    
    public ItemCardPrice()
    {
        On.uiInventoryItemDisplay.Show_ID_Transform_CharacterOverworld_Mode_string_Texture_bool_int_uiLoreCard_bool_bool += 
            OnItemPopover;
    }

    private GameObject OnItemPopover(
        On.uiInventoryItemDisplay.orig_Show_ID_Transform_CharacterOverworld_Mode_string_Texture_bool_int_uiLoreCard_bool_bool orig, 
        uiInventoryItemDisplay self, FTK_itembase.ID _itemid, Transform _lastowner, CharacterOverworld _cow, uiItemDetail.Mode _mode, 
        string _cameraid, Texture _textureoverride, bool _isinventory, int _amount, uiLoreCard _lorecard, bool _showequip, bool _forcefrontside
    ) {
        GameObject r = null;
        try
        {
            r = orig(self, _itemid, _lastowner, _cow, _mode, _cameraid, _textureoverride, _isinventory, _amount,
                _lorecard, _showequip, _forcefrontside);

            if (OptionsManager.ShowBasePrice)
            {
                UpdatePrice(self, _itemid, _cow);
            }
        }
        catch (Exception ex)
        {
            Plugin.Log.LogError(ex);
        }

        return r;
    }

    private void UpdatePrice(uiInventoryItemDisplay itemDisplay, FTK_itembase.ID itemId, CharacterOverworld cow)
    {
        var itembase = FTK_itembase.GetItemBase(itemId);
        var tuple = GetPricePanel((RectTransform)itemDisplay.gameObject.transform);
        var panel = tuple.Panel;
        var txt = tuple.Text;

        // if item isn't sellable, disabled price panel
        if (GameLogic.Instance.m_CantSellOrDiscardItems.Contains(itemId))
        {
            panel.SetActive(false);
            return;
        }
        
        // if in POI where sell is possible, display real sell price
        var poi = cow.m_HexLand.m_POI;
        if (poi != null && poi.CanSellItems())
        {
            panel.SetActive(true);
            var sellValue = GetSellValue(itemId, cow, poi);
            if (sellValue == 0)
            {
                panel.SetActive(false);
                return;
            }
            
            txt.text = sellValue.ToString();
            txt.color = CanSellColor;
            return;
        }
        
        // if in overworld, where sell isn't possible, display base price
        var price = CalculateBasePrice(itembase);
        if (price == 0)
        {
            panel.SetActive(false);
        }
        else
        {
            panel.SetActive(true);
            txt.text = price.ToString();
            txt.color = VisualParams.Instance.m_ColorTints.m_Gold;
        }
    }
    
    private int GetSellValue(FTK_itembase.ID item, CharacterOverworld cow, MiniHexInfo poi)
    {
        if (FTK_weaponStats2DB.GetDB().IsContain(item))
            return FTK_weaponStats2DB.Get(item).GetSellValue(cow, poi);
        else if (FTK_itemsDB.GetDB().IsContain(item))
            return FTK_itemsDB.Get(item).GetSellValue(cow, poi);
        else
            return 0;
    }

    private int CalculateBasePrice(FTK_itembase itembase)
    {
        return FTKUtil.RoundToInt(FTKUtil.Price(itembase._goldValue, 1, itembase.m_PriceScale) *
                                  GameFlow.Instance.GameDif.m_ItemSellValue);
    }

    private PricePanelTuple GetPricePanel(RectTransform parent)
    {
        var existingPanelTransform = parent.Find(PanelName);
        Text txt;
        
        if (existingPanelTransform != null && existingPanelTransform.gameObject != null)
        {
            txt = existingPanelTransform.Find(LabelName).gameObject.GetComponent<Text>();
            return new(existingPanelTransform.gameObject, txt);
        }
        
        Plugin.Log.LogInfo("Creating new price label..");

        // Creating PANEL
        var panel = new GameObject(PanelName).SetUILayer();
        var transform = panel.AddComponent<RectTransform>();
        
        transform.SetParent(parent);
        transform.anchorMin = new Vector2(1, 0);
        transform.anchorMax = new Vector2(1, 0);
        transform.pivot = new Vector2(1, 0);
        transform.anchoredPosition = new Vector2(-12, 12);
        // for some reason it become scaled to .5, just compensating for it
        transform.Scale1();

        CreateCoinsIcon(transform);
        txt = CreatePriceLabel(transform);
        
        Plugin.Log.LogInfo("Creating new price label DONE!");
        return new(panel, txt);
    }

    private void CreateCoinsIcon(RectTransform parent)
    {
        // Creating ICON
        var obj = new GameObject("coins-icon").SetUILayer();
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
        var obj = new GameObject(LabelName).SetUILayer();
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

        return txt;
    }
    
    private struct PricePanelTuple
    {
        public GameObject Panel;
        public Text Text;

        public PricePanelTuple(GameObject panel, Text text)
        {
            this.Panel = panel;
            this.Text = text;
        }
    }
}