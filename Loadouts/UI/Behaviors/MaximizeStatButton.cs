using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace AmadarePlugin.Loadouts.UI.Behaviors;

public class MaximizeStatButton : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
{
    private Button button;
    private Image image;
    public uiToolTipGeneral tooltip;
    public Sprite sprite;
    public StatType stat;
    private Action<StatType> callback;
    private Action<StatType> onHover;
    private Action<StatType> onBlur;


    public void Init(Sprite sprite, uiToolTipGeneral tooltip, StatType stat, Action<StatType> onClick, Action<StatType> onHover, Action<StatType> onBlur)
    {
        this.sprite = sprite;
        this.tooltip = tooltip;
        this.stat = stat;
        this.callback = onClick;
        this.onHover = onHover;
        this.onBlur = onBlur;
    }
    
    void Start()
    {
        this.button = this.gameObject.AddComponent<Button>();
        this.image = this.gameObject.AddComponent<Image>();
        this.image.type = Image.Type.Simple;
        this.button.transition = Selectable.Transition.ColorTint;
        this.button.targetGraphic = this.image;
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
        this.button.navigation = new Navigation
        {
            mode = Navigation.Mode.None
        };
        this.image.sprite = this.sprite;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        this.callback?.Invoke(this.stat);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        this.onHover?.Invoke(this.stat);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        this.onBlur?.Invoke(this.stat);
    }

    public void UpdateTooltip(int mod)
    {
        this.tooltip.m_Info = $"Maximize {stat:G} (<color=green>{mod}</color>)";
    }
}