namespace AmadarePlugin.Common;

public static class FtkHelpers
{
    public static CharacterOverworld InventoryOwner => FTKUI.Instance.m_PlayerInventory.m_InventoryOwner;
    public static string InventoryOwnerName => FTKUI.Instance.m_PlayerInventory.m_InventoryOwner.GetCowUniqueName();
    public static bool IsInventoryOpen => uiPlayerInventory.Instance.m_IsShowing;

    public static string InventoryCharacterName =>
        FTKUI.Instance.m_PlayerInventory.m_InventoryOwner.m_CharacterStats.m_CharacterName;

    public static bool IsInventoryOwner => FTKUI.Instance.m_PlayerInventory.m_InventoryOwner.IsOwner || GameLogic.Instance.IsSinglePlayer();

    public static bool IsActivePlayer
    {
        get
        {
            var cow = FTKUI.Instance.m_PlayerInventory.m_InventoryOwner;

            var isControllable = (GameLogic.Instance.IsSinglePlayer() || GameLogic.Instance.IsLocalMultiplayer()) ||
                                 cow.IsOwner ||
                                 cow.m_WaitForRespawn;
            return isControllable;
        }
    }

    public static readonly PlayerInventory.ContainerID[] EquipableContainers =
    {
        PlayerInventory.ContainerID.RightHand,
        PlayerInventory.ContainerID.LeftHand,
        PlayerInventory.ContainerID.Body,
        PlayerInventory.ContainerID.Head,
        PlayerInventory.ContainerID.Foot,
        PlayerInventory.ContainerID.Trinket,
        PlayerInventory.ContainerID.Neck,
    };

    public static string GetCowUniqueName(this CharacterOverworld cow)
    {
        var idx = FTKHub.Instance.m_CharacterOverworlds.IndexOf(cow);
        if (idx == -1)
        {
            Plugin.Log.LogError($"Cannot find COW '{cow.m_PlayerName}'");
        }

        return $"COW_IDX_{idx}";
    }
}