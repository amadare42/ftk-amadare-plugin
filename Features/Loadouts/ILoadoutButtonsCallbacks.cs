using LoadoutDict = System.Collections.Generic.Dictionary<PlayerInventory.ContainerID, GridEditor.FTK_itembase.ID>;

namespace AmadarePlugin.Features.Loadouts;

public interface ILoadoutButtonsCallbacks
{
    void LoadSlot(int idx);
    void SaveSlot(int idx);
    void ClearSlot(int idx);

    void LoadLoadout(CharacterOverworld cow, LoadoutDict loadout, bool isDistributed);
}