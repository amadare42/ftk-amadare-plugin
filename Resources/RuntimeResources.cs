using System.Collections.Generic;
using System.Linq;
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
        "iconLuck",
        
        "backingArrow",
        "arrowRB"
    };
    
    private static readonly Dictionary<string, string> RenamedRuntimeResourcesMap = new()
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

    public static void Init()
    {
        var sprites = UnityEngine.Resources.FindObjectsOfTypeAll<Sprite>();
        SpriteKeys.AddRange(RenamedRuntimeResourcesMap.Values);
        var renamedCount = 0;

        foreach (var obj in sprites)
        {
            if (SpriteKeys.Contains(obj.name))
            {
                dict[obj.name] = obj;
                if (RenamedRuntimeResourcesMap.ContainsValue(obj.name))
                {
                    dict[RenamedRuntimeResourcesMap.First(p => p.Value == obj.name).Key] = obj;
                    renamedCount++;
                }
            }
        }
        
        Plugin.Log.LogInfo($"Found {dict.Count - renamedCount}(+{renamedCount})/{SpriteKeys.Count} runtime Sprite resources.");
    }
}