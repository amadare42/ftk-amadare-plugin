using System;
using GridEditor;

namespace AmadarePlugin.Loadouts;

public class AssignToAllLoadoutsButton
{
    public event Action<FTK_itembase> OnAssignToAllLoadoutsClick;

    public static class CustomUiPopupMenuAction
    {
        public static uiPopupMenu.Action SaveToAllLoadouts = uiPopupMenu.Action.Close + 1;
        public static string SaveToAllLoadoutsText = "Set to all loadouts";
    }
    
    public AssignToAllLoadoutsButton()
    {
        // create button
        On.uiPopupMenu.Awake += UiPopupMenuOnAwake;
        // make button always show
        On.uiPopupMenu.ShowPlayerEquiped += (orig, self) =>
        {
            orig(self);
            self.ShowButton(CustomUiPopupMenuAction.SaveToAllLoadouts, self.m_CanControl);
        };
        // On.uiPopupMenu.Show += UiPopupMenuOnShow;
        // attach handler
        On.uiPopupMenu.OnClick += UiPopupMenuOnOnClick;
    }

    private void UiPopupMenuOnOnClick(On.uiPopupMenu.orig_OnClick orig, uiPopupMenu self, uiPopupMenu.Action _a, int _giveindex)
    {
        if (_a == CustomUiPopupMenuAction.SaveToAllLoadouts)
        {
            this.OnAssignToAllLoadoutsClick?.Invoke(self.m_LastItemIcon.ItemIcon.m_ItemInfo);
            return;
        }
        orig(self, _a, _giveindex);
    }

    private void UiPopupMenuOnAwake(On.uiPopupMenu.orig_Awake orig, uiPopupMenu self)
    {
        var action = CustomUiPopupMenuAction.SaveToAllLoadouts;
        var text = CustomUiPopupMenuAction.SaveToAllLoadoutsText;
        
        // add popup
        var nPopups = new uiPopupMenu.PopupButton[self.m_Popups.Length + 1];
        Array.Copy(self.m_Popups, nPopups, self.m_Popups.Length);
        self.m_Popups = nPopups;
        var newButtonIdx = nPopups.Length - 1;
        nPopups[newButtonIdx] = new uiPopupMenu.PopupButton
        {
            m_Action = action,
            m_DisplayName = text,
            m_Count = 1
        };

        orig(self);
    }
}