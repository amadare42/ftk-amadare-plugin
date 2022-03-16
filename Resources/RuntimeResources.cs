using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace AmadarePlugin.Resources;

public static class RuntimeResources
{
    private static Dictionary<string, Object> dict = new();
    
    public static bool IsInited { get; private set; }

    private static List<string> SpriteKeys = new() {
        "iconAwareness",
        "iconSpeed",
        "iconVitality",
        "iconIntelligence",
        "iconStrength",
        "iconTalent",
        "iconLuck",
        
        "backingArrow",
        "arrowRB",
        "coins"
    };
    private static List<string> FontKeys = new() {
        "Kingthings Petrock"
    };
    
    private static readonly Dictionary<string, string> RenamedSprites = new()
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

    public static T Get<T>(string key) where T : Object
    {
        AssertInited();
        return (T)dict[key];
    }

    public static void AssertInited()
    {
        if (!IsInited)
        {
            Plugin.Log.LogDebug("AssertInited: Start init");
            Init();
        }
    }

    private static void Init()
    {
        var renamedCount = 0;
        
        // sprites
        SpriteKeys.AddRange(RenamedSprites.Values);
        Load<Sprite>(SpriteKeys, ref renamedCount);

        // font
        Load<Font>(FontKeys, ref  renamedCount);
        
        Plugin.Log.LogInfo($"Found {dict.Count - renamedCount}(+{renamedCount} renamed)/{SpriteKeys.Count + FontKeys.Count} runtime resources.");
        IsInited = true;
    }

    private static void Load<T>(List<string> keys, ref int renamedCount) where T : Object
    {
        var objects = UnityEngine.Resources.FindObjectsOfTypeAll<T>();

        foreach (var obj in objects)
        {
            if (keys.Contains(obj.name))
            {
                dict[obj.name] = obj;
                if (RenamedSprites.ContainsValue(obj.name))
                {
                    dict[RenamedSprites.First(p => p.Value == obj.name).Key] = obj;
                    renamedCount++;
                }
            }
        }
    }
}