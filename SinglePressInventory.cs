using AmadarePlugin.Options;

namespace AmadarePlugin;

public static class SinglePressInventory
{
    public static void Init()
    {
        On.CharacterOverworld.CheckInput += (orig, self) =>
        {
            if (!OptionsManager.InventoryOnSinglePress)
            {
                orig(self);
                return;
            }
            if (self.m_InputFocus.GetButtonDown("Inventory"))
                self.StartCoroutine(self.InventoryToggleSequence(true));
            if (!OverworldCamera.Instance.m_Camera.enabled)
                return;
            self.OverworldCameraControl();
        };
    }
}