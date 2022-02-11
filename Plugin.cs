using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using AmadarePlugin.InventoryPresets;
using BepInEx;
using BepInEx.Logging;
using Google2u;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace AmadarePlugin
{
    [BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    public class Plugin : BaseUnityPlugin
    {
        public static Plugin Instance;
        public static ManualLogSource Log => Plugin.Instance.Logger;

        private InventoryPresetManager inventoryPresetManager;
        
        private void Awake()
        {
            Instance = this;
            Logger.LogInfo($"Plugin {PluginInfo.PLUGIN_GUID} is loaded!");

            On.CharacterStats.GetXpDisplayString += (_, self) => GetXpDisplayString(self);
            On.uiPlayerStats.UpdateDisplay += (orig, self, cow) =>
            {
                orig(self, cow);
                UpdatePoisonDisplay(self, cow);
            };
            On.uiPlayerMainHud.OpenInventory += (orig, self) =>
            {
                orig(self);
            };
            On.uiPlayerInventory.ShowCharacterInventory += (orig, self, cow, cycler) =>
            {
                orig(self, cow, cycler);
                LogInventory(cow);
            };
            inventoryPresetManager = new InventoryPresetManager();
            this.inventoryPresetManager.Init();
            
            Logger.LogInfo($"Patched!");
        }

        private static void LogInventory(CharacterOverworld cow)
        {
            var inventory = cow.m_PlayerInventory;
            var containers = inventory.Field<PlayerInventory, Dictionary<PlayerInventory.ContainerID, ItemContainer>>("m_Containers");
            var sb = new StringBuilder();
            
            foreach (var pair in containers)
            {
                sb.Append(pair.Key.ToString("G"))
                    .Append(": ");
                foreach (var itemPair in pair.Value.m_CountDictionary)
                {
                    if (itemPair.Value > 0)
                    {
                        sb.Append(FTKHub.Localized<TextItems>("STR_" + itemPair.Key)).Append(" ");
                    }
                }

                sb.AppendLine();
            }
            
            Instance.Logger.LogInfo(sb.ToString());
        }

        private static void UpdatePoisonDisplay(uiPlayerStats stats, CharacterOverworld cow)
        {
            if (cow.m_CharacterStats.IsPoisoned)
            {
                var text = stats.m_PoisonAilment.m_Text.text + " (" + (3 - (int)cow.m_CharacterStats.GetType().GetField("m_PoisonTimeCounter", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(cow.m_CharacterStats)) + " turns)";
                stats.m_PoisonAilment.SetTextValue(text);
            }
        } 

        private static string GetXpDisplayString(CharacterStats stats)
        {
            string xpDisplayString;
            if (stats.m_CharacterOverworld.m_CharacterStats.m_PlayerLevel < GameFlow.Instance.m_MaxCharacterLevels)
            {
                // CHANGE: xp bar display
                var currentLevel = stats.m_CharacterOverworld.m_CharacterStats.m_PlayerLevel;
                if (currentLevel == 0)
                {
                    xpDisplayString = stats.m_CharacterOverworld.m_CharacterStats.m_PlayerXP + " / " + GameFlow.Instance.m_LevelXpValues[0] + " (" + stats.m_CharacterOverworld.m_CharacterStats.m_PlayerXP  + ")";
                }
                else
                {
                    var xpTotal = stats.m_CharacterOverworld.m_CharacterStats.m_PlayerXP;
                    var prevLvlThreshold = GameFlow.Instance.m_LevelXpValues[currentLevel - 1];
                    var xpToNextLevel = GameFlow.Instance.m_LevelXpValues[currentLevel] - GameFlow.Instance.m_LevelXpValues[currentLevel - 1];
                    var xpInThisLvl = xpTotal - prevLvlThreshold;

                    xpDisplayString = string.Concat(xpInThisLvl, " / ", xpToNextLevel, " (", xpTotal, ")");
                }
                // -------------------------
            }
            else
            {
                int levelXpValue = GameFlow.Instance.m_LevelXpValues[stats.m_CharacterOverworld.m_CharacterStats.m_PlayerLevel];
                xpDisplayString = levelXpValue + " / " + levelXpValue;
            }
            return xpDisplayString;
        }
    }
}
