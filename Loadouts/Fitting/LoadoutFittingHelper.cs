using AmadarePlugin.Extensions;
using GridEditor;
using UnityEngine;

namespace AmadarePlugin.Loadouts.Fitting;

using LoadoutDict = System.Collections.Generic.Dictionary<PlayerInventory.ContainerID, GridEditor.FTK_itembase.ID>;

public class LoadoutFittingHelper
{
    public static void ResetDisplay(CharacterOverworld cow)
    {
        cow.m_UIPlayMainHud.UpdateHud();
    }

    public static FittingCharacterStats CreateFittingCharacterStats(CharacterOverworld cow, LoadoutDict loadout, bool amend = false)
    {
        var dummyStats = CreateFittingDummy(cow, loadout, amend);
        dummyStats.Loadout = loadout;
        dummyStats.Recalculate();

        return dummyStats;
    }

    public static void TestFit(CharacterOverworld cow, LoadoutDict loadout)
    {
        var dummyStats = CreateFittingCharacterStats(cow, loadout);
        TestFit(dummyStats);
    }

    public static void TestFit(FittingCharacterStats stats)
    {
        UpdateDisplay(stats, GetWeapon(stats.Loadout));
        Plugin.Log.LogInfo($"Test fit loadout.");
    }
    

    private static FittingCharacterStats CreateFittingDummy(CharacterOverworld cow, LoadoutDict loadout, bool amend = false)
    {
        var stats = cow.m_CharacterStats;
        // ReSharper disable once Unity.IncorrectMonoBehaviourInstantiation
        var dummyStats = new FittingCharacterStats();
        dummyStats.SetStats(stats, cow);

        // removing all equipped items from dummy
        foreach (var pair in cow.m_PlayerInventory.GetValidEquipmentContainers())
        {
            var container = pair.Value;
            if (!container.IsEmpty() && (!amend || loadout.ContainsKey(container.m_ID)))
            {
                var equippedItemId = container.GetOne();
                if (amend)
                {
                    if (!loadout.ContainsKey(container.m_ID))
                    {
                        var isSingleHandEquipped = container.m_ID == PlayerInventory.ContainerID.RightHand || container.m_ID == PlayerInventory.ContainerID.LeftHand;
                        if (isSingleHandEquipped && loadout.TryGetValue(PlayerInventory.ContainerID.RightHand, out var item) && FTK_itembase.GetItemBase(item).m_ObjectSlot == FTK_itembase.ObjectSlot.twoHands)
                        {
                            // 1 or 2 single handed items quipped but loadout have two handed item - remove item
                            dummyStats.AddOrRemoveCharacterModifierEx(equippedItemId, false);
                        }
                        else
                        {
                            var twoHandsEquipped = FTK_itembase.GetItemBase(equippedItemId).m_ObjectSlot == FTK_itembase.ObjectSlot.twoHands;
                            var loadoutHaveSingleHandedItems =
                                loadout.ContainsKey(PlayerInventory.ContainerID.RightHand) ||
                                loadout.ContainsKey(PlayerInventory.ContainerID.LeftHand);
                            if (twoHandsEquipped && loadoutHaveSingleHandedItems)
                            {
                                // two-handed item is equipped, but loadout have 1 or 2 single handed items - remove item
                                dummyStats.AddOrRemoveCharacterModifierEx(equippedItemId, false);
                            }
                        }
                        
                        // item slot is empty in loadout, but may be filled on actual character - don't remove item mods 
                        continue;
                    }
                }
                dummyStats.AddOrRemoveCharacterModifierEx(equippedItemId, false);
            }
        }

        // equip all loadout items to dummy
        foreach (var pair in loadout)
        {
            var item = pair.Value;
            if (item != FTK_itembase.ID.None)
            {
                dummyStats.AddOrRemoveCharacterModifierEx(item, true);
            }
        }

        return dummyStats;
    }

    private static FTK_itembase.ID GetWeapon(LoadoutDict loadout)
    {
        if (loadout.TryGetValue(PlayerInventory.ContainerID.RightHand, out var id))
        {
            return id;
        }

        return FTK_itembase.ID.None;
    }

    private static void UpdateDisplay(FittingCharacterStats stats, FTK_itembase.ID weapon)
    {
        var ui = stats.m_CharacterOverworld.m_UIPlayMainHud;
        ui.m_UpdateHud = false;

        ui.m_PhysicalDef.text = (stats.TotalArmor + stats.PartyArmorMod).ToString();
        ui.m_EvadeDef.text = FTKUtil.RoundToInt((stats.EvadeRating + stats.PartyEvadeMod) * 100f).ToString();
        ui.m_MagicDef.text = (stats.TotalResist + stats.PartyResistMod).ToString();
        ui.SetHealthDisplay(stats.m_HealthCurrent, stats.MaxHealth);

        if (weapon != FTK_itembase.ID.None)
        {
            // only update weapon value if it is known
            ui.m_AttackPower.text = stats.GetWeaponMaxDamageEx(weapon).ToString();
            if (FTK_weaponStats2DB.GetDB().GetEntry(weapon)._dmgtype == FTK_weaponStats2.DamageType.magic)
                ui.m_AttackPower.color = VisualParams.Instance.m_ColorTints.m_MagDamageColor;
            else
                ui.m_AttackPower.color = Color.white;
        }

        var skillDisplay1 = stats.GetSkillDisplay(FTK_weaponStats2.SkillType.fortitude);
        var skillDisplay2 = stats.GetSkillDisplay(FTK_weaponStats2.SkillType.toughness);
        var skillDisplay3 = stats.GetSkillDisplay(FTK_weaponStats2.SkillType.quickness);
        var skillDisplay4 = stats.GetSkillDisplay(FTK_weaponStats2.SkillType.awareness);
        var skillDisplay5 = stats.GetSkillDisplay(FTK_weaponStats2.SkillType.vitality);
        var skillDisplay6 = stats.GetSkillDisplay(FTK_weaponStats2.SkillType.talent);
        var skillDisplay7 = stats.GetSkillDisplay(FTK_weaponStats2.SkillType.luck);
        ui.m_Fortitude.text = skillDisplay1.m_Value;
        ui.m_Fortitude.color = skillDisplay1.m_Color;
        ui.m_Toughness.text = skillDisplay2.m_Value;
        ui.m_Toughness.color = skillDisplay2.m_Color;
        ui.m_Quickness.text = skillDisplay3.m_Value;
        ui.m_Quickness.color = skillDisplay3.m_Color;
        ui.m_Awareness.text = skillDisplay4.m_Value;
        ui.m_Awareness.color = skillDisplay4.m_Color;
        ui.m_Vitality.text = skillDisplay5.m_Value;
        ui.m_Vitality.color = skillDisplay5.m_Color;
        ui.m_Talent.text = skillDisplay6.m_Value;
        ui.m_Talent.color = skillDisplay6.m_Color;
        ui.m_Luck.text = skillDisplay7.m_Value;
        ui.m_Luck.color = skillDisplay7.m_Color;
        ui.m_UpdateHud = false;
    }
}