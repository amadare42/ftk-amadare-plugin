using System;
using AmadarePlugin.Resources;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace AmadarePlugin.Features.Loadouts.UI.Behaviors;

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
        this.image.sprite = RuntimeResources.Get<Sprite>("arrowRB");
        
        this.button.transition = Selectable.Transition.ColorTint;
        this.button.targetGraphic = this.image;
        this.button.navigation = new Navigation
        {
            mode = Navigation.Mode.None
        };
        
        this.tooltip = this.gameObject.AddComponent<uiToolTipGeneral>();
        this.tooltip.m_IsFollowHoriz = true;
        this.tooltip.m_ReturnRawInfo = true;
        this.tooltip.m_Info = "Maximize Stats";
        this.tooltip.m_ToolTipOffset = new Vector2(0, -50);
        
        this.button.colors = new ColorBlock
        {
            highlightedColor = Color.white,
            normalColor = new Color(0.74f, 0.72f, 0.75f),
            pressedColor = new Color(0.74f, 0.72f, 0.75f),
            colorMultiplier = 1,
            disabledColor = new Color(0.74f, 0.72f, 0.75f),
            fadeDuration = .2f
        };
        this.image.color = new Color(0.75f, 0.68f, 0.52f);
    }

    public void UpdateState()
    {
        if (this.IsExpanded)
        {
            this.image.transform.rotation = Quaternion.AngleAxis(180, Vector3.forward);
            this.tooltip.m_Info = "Hide Maximize Stats";
        }
        else
        {
            this.image.transform.rotation = Quaternion.AngleAxis(0, Vector3.forward);
            this.tooltip.m_Info = "Show Maximize Stats";
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        this.IsExpanded = !this.IsExpanded;
        this.OnExpandedChanged?.Invoke(this.IsExpanded);
        UpdateState();
    }
}