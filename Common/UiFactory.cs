using AmadarePlugin.Extensions;
using UnityEngine;
using UnityEngine.UI;

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

    public static GameObject SetUILayer(this GameObject go)
    {
        go.layer = LayerMask.NameToLayer("UI");
        return go;
    }
}