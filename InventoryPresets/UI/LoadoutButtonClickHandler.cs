using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace AmadarePlugin.InventoryPresets.UI;

public class LoadoutButtonClickHandler : MonoBehaviour, IPointerClickHandler
{
    public Action<LoadoutButton, int, PointerEventData.InputButton> Callback;
    public int Index { get; set; }
    public LoadoutButton Button { get; set; }

    public void OnPointerClick(PointerEventData eventData)
    {
        this.Callback?.Invoke(Button, this.Index, eventData.button);
    }
}