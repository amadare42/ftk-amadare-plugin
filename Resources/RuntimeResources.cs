using System.Collections.Generic;
using UnityEngine;

namespace AmadarePlugin.Resources;

public static class RuntimeResources
{
    private static Dictionary<string, object> dict = new();

    private static List<string> SpriteKeys = new() {
        "iconAwareness",
        "iconSpeed",
        "iconVitality",
        "iconIntelligence",
        "iconStrength",
        "iconTalent",
        "backingArrow"
    };

    private static readonly Dictionary<string, string> loadoutButtonsMap = new()
    {
        { "disabled", "buttonGeneric1DIS" },
        { "empty_normal", "buttonGeneric2" },
        { "empty_hover", "buttonGeneric2HL" },
        { "empty_pressed", "buttonGeneric3DIS" },
        { "equipped_hover", "buttonGeneric4HL" },
        { "equipped_normal", "buttonGeneric4" },
        { "filled_hover", "buttonGeneric1HL" },
        { "filled_normal", "buttonGeneric1" },
        { "unavailable_hover", "buttonGeneric3HL" },
        { "unavailable_normal", "buttonGeneric3" },
    };

    public static T Get<T>(string key) => (T)dict[key];

    public static Sprite LoadButton(string name) => (Sprite)dict[loadoutButtonsMap[name]];

    public static void Init()
    {
        var sprites = UnityEngine.Resources.FindObjectsOfTypeAll<Sprite>();
        SpriteKeys.AddRange(loadoutButtonsMap.Values);

        foreach (var tex in sprites)
        {
            if (SpriteKeys.Contains(tex.name))
            {
                dict[tex.name] = tex;
            }
        }
        
        Plugin.Log.LogInfo($"Found {dict.Count}/{SpriteKeys.Count} runtime Sprite resources.");
    }
}