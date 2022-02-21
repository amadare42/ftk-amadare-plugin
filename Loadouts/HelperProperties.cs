namespace AmadarePlugin.Loadouts;

public class HelperProperties
{
    public static CharacterOverworld InventoryOwner => FTKUI.Instance.m_PlayerInventory.m_InventoryOwner;
    public static string InventoryOwnerName => FTKUI.Instance.m_PlayerInventory.m_InventoryOwner.m_PlayerName;
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
}