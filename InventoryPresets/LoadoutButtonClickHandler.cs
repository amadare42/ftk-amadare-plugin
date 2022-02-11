using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace AmadarePlugin.InventoryPresets;

public class LoadoutButtonClickHandler : MonoBehaviour, IPointerClickHandler
{
    public Action<int, PointerEventData.InputButton> Callback;
    public int Index { get; set; }

    public void OnPointerClick(PointerEventData eventData)
    {
        this.Callback?.Invoke(this.Index, eventData.button);
    }
}