using AmadarePlugin.Extensions;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace AmadarePlugin.Common;

public static class UiFactory
{
    public static GameObject CreateCheckBox(string name = null, string tooltipTitle = null, string tooltipDetails = null, bool value = false)
    {
        var prefab = uiOptionsMenu.Instance.m_DisplayOptions.m_TiltShiftToggle.gameObject;

        var checkbox = Object.Instantiate(prefab);
        checkbox.name = "custom checkbox";
        var transform = (RectTransform)checkbox.transform;
        transform.anchoredPosition = Vector2.zero;
        transform.pivot = new Vector2(0, .5f);
        transform.sizeDelta = Vector2.zero;

        var toggle = checkbox.GetComponent<Toggle>();
        toggle.isOn = value;
        toggle.onValueChanged = new Toggle.ToggleEvent();
        
        var text = checkbox.transform.Find("Label").GetComponent<Text>();
        if (tooltipTitle != null || tooltipDetails != null)
        {
            var tooltip = checkbox.AddComponent<uiToolTipGeneral>();
            tooltip.m_ReturnRawInfo = true;
            tooltip.m_Info = tooltipTitle;
            tooltip.m_DetailInfo = tooltipDetails;
        }
        text.text = name;

        return checkbox;
    }
    
    public static GameObject CreateMenuButton(string label, UnityAction callback, GameObject prefab = null)
    {
        prefab ??= uiOptionsMenu.Instance.m_ControlButton.gameObject;
        
        var btnObject = Object.Instantiate(prefab);
        btnObject.name = "custom button";
        var transform = (RectTransform)btnObject.transform;
        transform.Scale1();

        var textComponent = transform.Find("Text").GetComponent<Text>();
        textComponent.text = label;

        var buttonComponent = btnObject.GetComponent<Button>();
        buttonComponent.onClick = new Button.ButtonClickedEvent();
        buttonComponent.onClick.AddListener(callback);

        return btnObject;
    }

    public static GameObject SetUILayer(this GameObject go)
    {
        go.layer = LayerMask.NameToLayer("UI");
        return go;
    }
}