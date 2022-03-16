using System.Collections;
using AmadarePlugin.Extensions;
using AmadarePlugin.Options;
using On.GridEditor;

namespace AmadarePlugin.Features;

public class UiTweaks
{
    public UiTweaks()
    {
        On.CharacterStats.GetXpDisplayString += CharacterStatsOnGetXpDisplayString;
        On.CharacterStats.EndTurnActionSequence += OnEndTurnActionSequence;
        On.uiPlayerStats.UpdateDisplay += OnUpdateDisplay;
        On.GridEditor.FTK_miniEncounter.GetDisplayBottom += FTK_miniEncounterOnGetDisplayBottom;
        On.GridEditor.FTK_miniEncounter.GetDisplayName += FTK_miniEncounterOnGetDisplayName;
    }

    private string FTK_miniEncounterOnGetDisplayName(FTK_miniEncounter.orig_GetDisplayName orig, GridEditor.FTK_miniEncounter self)
    {
        if (OptionsManager.HighlightOneTimeEncounters)
        {
            return $"{orig(self)}{(self.m_DestroyOnLeave ? "!" : "")}";
        }

        return orig(self);
    }

    private string FTK_miniEncounterOnGetDisplayBottom(FTK_miniEncounter.orig_GetDisplayBottom orig, GridEditor.FTK_miniEncounter self)
    {
        if (OptionsManager.HighlightOneTimeEncounters)
        {
            return$"{orig(self)}\n<color=#555555>This encounter {(self.m_DestroyOnLeave ? "WILL" : "WILL NOT")} disappear on leave</color>";
        }

        return orig(self);
    }

    private IEnumerator OnEndTurnActionSequence(On.CharacterStats.orig_EndTurnActionSequence orig, CharacterStats self, ContinueFSM _cfsm)
    {
        foreach (var obj in orig(self, _cfsm).ToIEnumerable())
        {
            yield return obj;
        }
        
        UpdatePoisonDisplay(uiPlayerInventory.Instance.m_PlayerStats, self.m_CharacterOverworld);
    }
    

    private void OnUpdateDisplay(On.uiPlayerStats.orig_UpdateDisplay orig, uiPlayerStats self, CharacterOverworld cow)
    {
        orig(self, cow);
        UpdatePoisonDisplay(self, cow);
    }

    private static void UpdatePoisonDisplay(uiPlayerStats stats, CharacterOverworld cow)
    {
        if (cow.m_CharacterStats.IsPoisoned && OptionsManager.DisplayPoisonTurns)
        {
            var text = stats.m_PoisonAilment.m_Text.text + " (" + (3 - cow.m_CharacterStats.m_PoisonTimeCounter) + " turns)";
            stats.m_PoisonAilment.SetTextValue(text);
        }
    } 

    private string CharacterStatsOnGetXpDisplayString(On.CharacterStats.orig_GetXpDisplayString orig, CharacterStats self)
    {
        if (OptionsManager.CustomXpString)
        {
            return GetXpDisplayString(self);
        }

        return orig(self);
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