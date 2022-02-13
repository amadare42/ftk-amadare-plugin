using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace AmadarePlugin.InventoryPresets.UI.Behaviors;

public class LoadoutButtonPointerHandler : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
{
    public Action<LoadoutButton, int, PointerEventData.InputButton> Callback;
    public Action<LoadoutButton, int> HoverCallback;
    public Action<LoadoutButton, int> BlurCallback;
    public int Index { get; set; }
    public LoadoutButton Button { get; set; }

    public void OnPointerClick(PointerEventData eventData) => this.Callback?.Invoke(this.Button, this.Index, eventData.button);

    public void OnPointerEnter(PointerEventData eventData) => this.HoverCallback?.Invoke(this.Button, this.Index);

    public void OnPointerExit(PointerEventData eventData) => this.BlurCallback?.Invoke(this.Button, this.Index);
}