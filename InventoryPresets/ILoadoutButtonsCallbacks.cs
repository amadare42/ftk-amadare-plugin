namespace AmadarePlugin.InventoryPresets;

public interface ILoadoutButtonsCallbacks
{
    void LoadSlot(int idx);
    void SaveSlot(int idx);
    void ClearSlot(int idx);
}