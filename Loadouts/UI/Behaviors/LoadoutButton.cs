using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace AmadarePlugin.Loadouts.UI.Behaviors;

public class LoadoutButton : MonoBehaviour
{
    [HideInInspector]
    public static Dictionary<LoadoutButtonState, ButtonStateSprites> ButtonStatesMap = new Dictionary<LoadoutButtonState, ButtonStateSprites>();

    public LoadoutButtonState State = LoadoutButtonState.Disabled;

    private Button button;
    private Image image;
    public uiToolTipGeneral tooltip;

    void Awake()
    {
        this.button = this.gameObject.AddComponent<Button>();
        this.image = this.gameObject.AddComponent<Image>();
        this.image.type = Image.Type.Sliced;
        this.button.transition = Selectable.Transition.SpriteSwap;
        this.button.targetGraphic = this.image;
        this.button.navigation = new Navigation
        {
            mode = Navigation.Mode.None
        };
        SetState(LoadoutButtonState.Empty);
    }

    public void SetState(LoadoutButtonState state)
    {
        var sprites = ButtonStatesMap[state];
        this.image.sprite = sprites.Normal;
        
        this.button.spriteState = new SpriteState
        {
            highlightedSprite = sprites.Hover,
            pressedSprite = sprites.Pressed
        };
        this.State = state;
    }
}

public class ButtonStateSprites
{
    public Sprite Normal { get; set; }
    public Sprite Hover { get; set; }
    public Sprite Pressed { get; set; }

    public ButtonStateSprites(Sprite normal, Sprite hover, Sprite pressed)
    {
        this.Normal = normal;
        this.Hover = hover;
        this.Pressed = pressed;
    }

    public ButtonStateSprites(Sprite normal)
    {
        this.Normal = normal;
        this.Hover = normal;
        this.Pressed = normal;
    }
}

public enum LoadoutButtonState
{
    None,
    Disabled,
    Empty,
    Filled,
    Unavailable,
    Equipped
}