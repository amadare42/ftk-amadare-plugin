using System.Collections.Generic;
using AmadarePlugin.Common;
using AmadarePlugin.Extensions;
using BepInEx.Configuration;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace AmadarePlugin.Options;

public class PluginOptionsUi
{
    public List<ConfigEntryBase> Bindings = new();
    
    public void Init()
    {
        On.uiOptionsMenu.Show += (orig, self) => Utils.SafeInvoke(() => orig(self), () => CreatePluginOptions(self));
    }

    public void RegisterBindings(IEnumerable<ConfigEntryBase> bindings)
    {
        Bindings.AddRange(bindings);
    }

    private void CreatePluginOptions(uiOptionsMenu menu)
    {
        if (!menu.gameObject.transform.Find("MainWindow/plugin_options"))
        {
            Plugin.Log.LogInfo($"Creating plugin options button");
            var button = UiFactory.CreateMenuButton("Plugin Options", OpenPluginOptions);
            button.name = "plugin_options";
            button.transform.SetParent(menu.gameObject.transform.Find("MainWindow"));
            button.transform.SetSiblingIndex(7);
            button.transform.Scale1();
        }
    }

    private void OpenPluginOptions()
    {
        var existing = FTKHub.Instance.m_uiRoot.Find("SystemUI/OptionTarget/OptionMenu/PluginOptionsPanel");
        if (existing)
        {
            Plugin.Log.LogInfo($"Option exists, showing..");
            existing.gameObject.SetActive(true);
            return;
        }
        
        Plugin.Log.LogInfo($"Opening options..");
        var panelTransform = (RectTransform)Object.Instantiate(FTKHub.Instance.m_uiRoot.Find("SystemUI/OptionTarget/OptionMenu/GamePanel").transform);
        panelTransform.name = "PluginOptionsPanel";
        var panelGo = panelTransform.gameObject;

        panelTransform.Find("DisplayHeader/HeaderText").GetComponent<Text>().text = "Plugin Options";
        
        var optionsGo = panelTransform.Find("Options");
        checkboxPrefab = Object.Instantiate(optionsGo.transform.Find("LockCursor")).gameObject;
        
        // remove existing checkboxes
        foreach (Transform child in optionsGo.transform)
        {
            Object.Destroy(child.gameObject);
        }
        
        foreach (var bind in Bindings)
        {
            AddCheckbox(bind, (RectTransform)optionsGo.transform);
        }

        // var checkbox = CreateCheckbox("Inventory on single press", OptionsManager.InventoryOnSinglePress,
        //     value =>
        //     {
        //         OptionsManager.InventoryOnSinglePress = value;
        //         Plugin.Log.LogInfo($"InventoryOnSinglePress set to {value}");
        //     });
        panelTransform.SetParent(FTKHub.Instance.m_uiRoot.Find("SystemUI/OptionTarget/OptionMenu").transform);
        panelGo.SetActive(true);
        
        panelTransform.ScaleResolutionBased();
        panelTransform.anchoredPosition = new Vector2(0, -200);
        panelTransform.anchorMin = new Vector2(0.5f, 1);
        panelTransform.anchorMax = new Vector2(0.5f, 1);
    }

    private GameObject checkboxPrefab;

    private void AddCheckbox(ConfigEntryBase entryBase, RectTransform parent)
    {
        if (entryBase.SettingType == typeof(bool))
        {
            var checkbox = CreateCheckbox(
                entryBase.Definition.Key,
                (bool)entryBase.BoxedValue,
                v => entryBase.BoxedValue = v,
                entryBase.Description.Description);
            checkbox.transform.SetParent(parent);
        }
    }

    private GameObject CreateCheckbox(string label, bool isOn, UnityAction<bool> callback, string tooltipText = null)
    {
        var checkboxGo = Object.Instantiate(checkboxPrefab);
        var toggle = checkboxGo.GetComponent<Toggle>();
        toggle.isOn = isOn;
        toggle.onValueChanged = new Toggle.ToggleEvent();
        toggle.onValueChanged.AddListener(callback);
        
        checkboxGo.transform.Find("Label").GetComponent<Text>().text = label;

        // tooltips are located in layer below menu, so they will not display correctly
        // if (tooltipText != null)
        // {
        //     var tooltip = checkboxGo.AddComponent<uiToolTipGeneral>();
        //     tooltip.m_Info = label;
        //     tooltip.m_ReturnRawInfo = true;
        //     tooltip.m_DetailInfo = tooltipText;
        //     tooltip.m_IsFollowHoriz = true;
        //     tooltip.m_ToolTipOffset = new Vector2(0, -50);
        // }

        return checkboxGo;
    }
}