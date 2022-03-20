using AmadarePlugin.Extensions;

namespace AmadarePlugin.Common;

public static class FtkHelpers
{
    public static CharacterOverworld InventoryOwnerCow => FTKUI.Instance.m_PlayerInventory.m_InventoryOwner;
    public static string InventoryOwnerUniqueName => InventoryOwnerCow.GetCowUniqueName();
    public static bool IsInventoryOpen => uiPlayerInventory.Instance.m_IsShowing;


    public static CharacterOverworld FindByUniqueName(string uniqueName)
    {
        const string prefix = "COW_IDX_";
        if (uniqueName.StartsWith(prefix) 
            && int.TryParse(uniqueName.Substring(prefix.Length), out int idx)
            && idx >= 0
            && FTKHub.gInstance.IsOk()
            && FTKHub.Instance.m_CharacterOverworlds.Count > idx
        )
        {
            return FTKHub.Instance.m_CharacterOverworlds[idx];
        }
        
        Plugin.Log.LogWarning($"Cannot find COW by unique name '{uniqueName}'.");

        return null;
    }

    public static bool IsInventoryOwner => InventoryOwnerCow.IsOwner || GameLogic.Instance.IsSinglePlayer();

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