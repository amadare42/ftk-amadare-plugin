using System;
using AmadarePlugin.Resources;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace AmadarePlugin.InventoryPresets.UI.Behaviors;

public class ShowMoreButton : MonoBehaviour, IPointerClickHandler
{
    private Button button;
    private Image image;
    public uiToolTipGeneral tooltip;

    public bool IsExpanded { get; private set; }
    
    public event Action<bool> OnExpandedChanged;

    void Awake()
    {
        this.button = this.gameObject.AddComponent<Button>();
        this.image = this.gameObject.AddComponent<Image>();
        this.image.type = Image.Type.Sliced;
        this.image.sprite = RuntimeResources.Get<Sprite>("backingArrow");
        
        this.button.transition = Selectable.Transition.ColorTint;
        this.button.targetGraphic = this.image;
        this.button.navigation = new Navigation
        {
            mode = Navigation.Mode.None
        };
        
        this.tooltip = this.gameObject.AddComponent<uiToolTipGeneral>();
        this.tooltip.m_IsFollowHoriz = true;
        this.tooltip.m_ReturnRawInfo = true;
        this.tooltip.m_Info = "Show Skills";
        this.tooltip.m_ToolTipOffset = new Vector2(0, -50);
    }

    public void UpdateState()
    {
        if (IsExpanded)
        {
            this.image.transform.rotation = Quaternion.AngleAxis(180, Vector3.forward);
            this.tooltip.m_Info = "Hide Skills";
        }
        else
        {
            this.image.transform.rotation = Quaternion.AngleAxis(0, Vector3.forward);
            this.tooltip.m_Info = "Show Skills";
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        this.IsExpanded = !this.IsExpanded;
        OnExpandedChanged?.Invoke(this.IsExpanded);
        UpdateState();
    }
}