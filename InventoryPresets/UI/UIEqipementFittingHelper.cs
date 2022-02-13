using System.Collections.Generic;
using System.Linq;
using GridEditor;
using UnityEngine;

namespace AmadarePlugin.InventoryPresets.UI;
using LoadoutDict = System.Collections.Generic.Dictionary<PlayerInventory.ContainerID, GridEditor.FTK_itembase.ID>;

public class UIEqipementFittingHelper
{
    public static void Reset(CharacterOverworld cow)
    {
        cow.m_UIPlayMainHud.UpdateHud();
    }
    
    public static void TestFit(CharacterOverworld cow, LoadoutDict loadout)
    {
        var dummyStats = CreateFittingDummy(cow, loadout);
        dummyStats.Recalculate();

        UpdateDisplay(dummyStats, GetWeapon(loadout));
        Plugin.Log.LogInfo($"Test fit loadout.");
    }

    private static FittingCharacterStats CreateFittingDummy(CharacterOverworld cow, LoadoutDict loadout)
    {
        var stats = cow.m_CharacterStats;
        // ReSharper disable once Unity.IncorrectMonoBehaviourInstantiation
        var dummyStats = new FittingCharacterStats();
        dummyStats.SetStats(stats, cow);
        
        // removing all equipped items from dummy
        foreach (var pair in cow.m_PlayerInventory.GetValidEquipmentContainers())
        {
            var container = pair.Value;
            if (!container.IsEmpty())
            {
                dummyStats.AddOrRemoveCharacterModifierEx(container.GetOne(), false);
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
        
        ui.m_AttackPower.text = stats.GetWeaponMaxDamageEx(weapon).ToString();
        if (FTK_weaponStats2DB.GetDB().GetEntry(weapon)._dmgtype == FTK_weaponStats2.DamageType.magic)
            ui.m_AttackPower.color = VisualParams.Instance.m_ColorTints.m_MagDamageColor;
        else
            ui.m_AttackPower.color = Color.white;
        
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

    public class FittingCharacterStats : CharacterStats
    {
        public new List<FTK_characterModifier.ID> m_CharacterMods
        {
            get => base.m_CharacterMods;
            set => base.m_CharacterMods = value;
        }

        public void SetStats(CharacterStats stats, CharacterOverworld cow)
        {
            // GENERAL

            this.m_CharacterOverworld = cow;
            this.m_CharacterMods = stats.m_CharacterMods.ToList();
            this.m_HealthCurrent = stats.m_HealthCurrent;
            this.m_ActiveCurses = stats.m_ActiveCurses?.ToList() ?? new();
            this.m_PermaCurses = stats.m_PermaCurses?.ToList() ?? new();
            this.m_PoisonLvl = stats.m_PoisonLvl;
            this.m_PlayerLevel = stats.m_PlayerLevel;
            
            // SKILLS
            this.m_BaseToughness = stats.m_BaseToughness;
            this.m_ModToughness = stats.m_ModToughness;
            this.m_AugmentedToughness = stats.m_AugmentedToughness;

            this.m_BaseFortitude = stats.m_BaseFortitude;
            this.m_ModFortitude = stats.m_ModFortitude;
            this.m_AugmentedFortitude = stats.m_AugmentedFortitude;

            this.m_BaseAwareness = stats.m_BaseAwareness;
            this.m_ModAwareness = stats.m_ModAwareness;
            this.m_AugmentedAwareness = stats.m_AugmentedAwareness;

            this.m_BaseTalent = stats.m_BaseTalent;
            this.m_ModTalent = stats.m_ModTalent;
            this.m_AugmentedTalent = stats.m_AugmentedTalent;

            this.m_BaseVitality = stats.m_BaseVitality;
            this.m_ModVitality = stats.m_ModVitality;
            this.m_AugmentedVitality = stats.m_AugmentedVitality;

            this.m_BaseQuickness = stats.m_BaseQuickness;
            this._ModQuickness = stats._ModQuickness;
            this.m_AugmentedQuickness = stats.m_AugmentedQuickness;

            this.m_ModLuck = stats.m_ModLuck;
            this.m_AugmentedLuck = stats.m_AugmentedLuck;
            
            // DAMAGE
            
            this.m_AugmentedDamageMagic = stats.m_AugmentedDamageMagic;
            this.m_ModAttackMagic = stats.m_ModAttackMagic;
            
            this.m_AugmentedDamagePhysical = stats.m_AugmentedDamagePhysical;
            this.m_ModAttackPhysical = stats.m_ModAttackPhysical;
            
            this.m_ModAttackAll = stats.m_ModAttackAll;
            this.m_IsInCombat = stats.m_IsInCombat;
            
            // DEFENCE
            
            this.m_DiseaseLvl = stats.m_DiseaseLvl;
            typeof(CharacterStats).GetProperty(nameof(MyDisease))
                .SetValue(this, stats.MyDisease, null);

            this.m_BaseDefensePhysical = stats.m_BaseDefensePhysical;
            this.m_ModDefensePhysical = stats.m_ModDefensePhysical;
            this.m_AugmentedDefensePhysical = stats.m_AugmentedDefensePhysical;
            
            this.m_BaseEvadeRating = stats.m_BaseEvadeRating;
            this.m_ModEvadeRating = stats.m_ModEvadeRating;
            this.m_AugmentedEvadeRating = stats.m_AugmentedEvadeRating;

            this.m_BaseDefenseMagic = stats.m_BaseDefenseMagic;
            this.m_ModDefenseMagic = stats.m_ModDefenseMagic;
            this.m_AugmentedDefenseMagic = stats.m_AugmentedDefenseMagic;
            
            // HEALTH

            this.m_HealthCurrent = stats.m_HealthCurrent;
            this.m_BaseMaxHealth = stats.m_BaseMaxHealth;
            this.m_ModMaxHealth = stats.m_ModMaxHealth;
            this.m_AugmentedMaxHealth = stats.m_AugmentedMaxHealth;
            this.m_MaxHealthOverride = stats.m_MaxHealthOverride;
        }
        
        public int GetWeaponMaxDamageEx(FTK_itembase.ID weapon, FTK_enemyCombat.EnemyRace[] _againstRaces = null)
        {
            FTK_weaponStats2 entry = FTK_weaponStats2DB.GetDB().GetEntry(weapon);
            if (entry == null)
            {
                // TODO: this is incorrect, find out how to calculate bare-hands damage
                entry = new FTK_weaponStats2
                {
                    _maxdmg = 0,
                    _dmggain = 1,
                    _dmgtype = FTK_weaponStats2.DamageType.physical
                };
            }
            
            var baseMaxDmg = entry._dmggain * (float) this.m_PlayerLevel + entry._maxdmg;
            if (entry._dmgtype == FTK_weaponStats2.DamageType.magic)
                baseMaxDmg += (float) (this.m_ModAttackMagic + this.m_AugmentedDamageMagic);
            else if (entry._dmgtype == FTK_weaponStats2.DamageType.physical)
                baseMaxDmg += (float) (this.m_ModAttackPhysical + this.m_AugmentedDamagePhysical);
            float num2 = baseMaxDmg + (float) this.m_ModAttackAll;
            // List<FTK_enemyCombat.EnemyRace> enemyRaceList = new List<FTK_enemyCombat.EnemyRace>();
            float num3 = 0.0f;
            // if (_againstRaces != null)
            // {
            //     enemyRaceList = FTKUtil.ArrayToList<FTK_enemyCombat.EnemyRace>(_againstRaces);
            //     foreach (FTK_enemyCombat.EnemyRace againstRace in _againstRaces)
            //     {
            //         if (this.m_DamageAgainstBonus.ContainsKey(againstRace))
            //             num3 += this.m_DamageAgainstBonus[againstRace];
            //     }
            // }
            float num4 = num2 * (1f + num3);
            // if (this.m_IsInCombat)
            //     num4 *= 1f + this.m_CharacterOverworld.m_CurrentDummy.AttackDmgMod;
            float num5 = 1f + GameFlow.Instance.GetChaosAttackDamageMultiplier();
            return FTKUtil.RoundToInt(num4 * num5);
        }

        public int PartyArmorMod { get; set; }
        public int PartyResistMod { get; set; }
        public float PartyEvadeMod { get; set; }
        
        public void Recalculate()
        {
            this.TallyCharacterMods();
            this.TallyCharacterDefense();
            this.TallyCharacterHealth(this.m_PlayerLevel, false);
            Plugin.Log.LogInfo($"Party mods: {PartyArmorMod} {PartyResistMod} {PartyEvadeMod:0.000}");
        }
        
        public void AddOrRemoveCharacterModifierEx(FTK_itembase.ID _item, bool _add)
        {
            var mod = FTK_characterModifier.GetEnum(_item.ToString());
            var entry = FTK_characterModifierDB.GetDB().GetEntry(mod);
            
            if (_add)
            {
                if (FTK_characterModifierDB.GetDB().IsContainID(_item.ToString()))
                {
                    if (!this.m_CharacterMods.Contains(mod))
                    {
                        this.m_CharacterMods.Add(mod);
                        // using augmented field to reflect potential changes in party defense that is included in armor/resist calculations
                        PartyArmorMod += entry.m_PartyCombatArmor;
                        PartyResistMod += entry.m_PartyCombatResist;
                        PartyEvadeMod += entry.m_PartyCombatEvade;
                    }
                    
                }
            }
            else if (FTK_characterModifierDB.GetDB().IsContainID(_item.ToString()))
            {
                if (this.m_CharacterMods.Contains(mod))
                {
                    this.m_CharacterMods.Remove(mod);
                    PartyArmorMod -= entry.m_PartyCombatArmor;
                    PartyResistMod -= entry.m_PartyCombatResist;
                    PartyEvadeMod -= entry.m_PartyCombatEvade;
                }
            }
        }
    }
}