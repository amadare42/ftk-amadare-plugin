using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
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
        "Kingthings Petrock",
        "RobotoCondensed-Light"
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

        LoadEmbeddedIcons();
        
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
    
    private static void LoadEmbeddedIcons()
    {
        var prefix = "AmadarePlugin.Resources.Icons.";
        
        foreach (var manifestName in Assembly.GetExecutingAssembly().GetManifestResourceNames().Where(n => n.StartsWith(prefix)))
        {
            var sprite = LoadNewSprite(manifestName);
            var lookupName = manifestName
                .Replace(prefix, "")
                .Replace(".png", "");
            dict[lookupName] = sprite;
            Plugin.Log.LogInfo($"Loaded asset '{manifestName}' as '{lookupName}'");
        }
    }
    
    public static Sprite LoadNewSprite(string manifestName, float ppu = 100.0f)
    {
        var bytes = ToByteArray(Assembly.GetExecutingAssembly().GetManifestResourceStream(manifestName));
        var tex = LoadTexture(bytes);
        var sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height),new Vector2(.5f,.5f), ppu, 1, SpriteMeshType.FullRect);
        sprite.border.Set(5,5,5,5);
 
        return sprite;
    }
    
    public static byte[] ToByteArray(Stream input) => new BinaryReader(input).ReadBytes((int)input.Length);

    public static Texture2D LoadTexture(byte[] bytes) {
 
        var tex2D = new Texture2D(2, 2);
        if (tex2D.LoadImage(bytes))
            return tex2D;
        Plugin.Log.LogWarning("Error when loading load texture!");
        return null; 
    }
}