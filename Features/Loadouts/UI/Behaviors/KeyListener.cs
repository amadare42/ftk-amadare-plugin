using System;
using UnityEngine;

namespace AmadarePlugin.Features.Loadouts.UI.Behaviors;

public class KeyListener : MonoBehaviour
{
    private Action<KeyListener> onKeyPressedChanged;
    public bool IsShiftPressed { get; set; }
    

    public void Init(Action<KeyListener> onKeyPressChanged)
    {
        this.onKeyPressedChanged = onKeyPressChanged;
    }

    void Update()
    {
        var shiftPressed = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
        if (this.IsShiftPressed != shiftPressed)
        {
            this.IsShiftPressed = shiftPressed;
            this.onKeyPressedChanged?.Invoke(this);
        }
    }
}