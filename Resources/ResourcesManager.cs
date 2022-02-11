using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace AmadarePlugin.Resources;

public static class ResourcesManager
{
    private static Dictionary<string, object> assets = new();

    public static void Init()
    {
        LoadButtonTextures();
        // TODO: pack with correct unity version
        // LoadFromAssetBundle();
        Plugin.Log.LogInfo("Loaded assets done!");

        return;
    }

    private static void LoadFromAssetBundle()
    {
        using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("AmadarePlugin.Resources.main");
        var bundle = AssetBundle.LoadFromStream(stream);
        
        foreach (var name in bundle.GetAllAssetNames())
        {
            var lookupName = name.Replace("assets/resources/", "");
            assets[lookupName] = bundle.LoadAsset(name);
            
            Plugin.Log.LogInfo($"Loaded asset '{name}' as '{lookupName}'");
        }

        bundle.Unload(false);
    }

    private static void LoadButtonTextures()
    {
        var prefix = "AmadarePlugin.Resources.buttons.";
        
        foreach (var manifestName in Assembly.GetExecutingAssembly().GetManifestResourceNames().Where(n => n.StartsWith(prefix)))
        {
            var sprite = LoadNewSprite(manifestName);
            var lookupName = manifestName.Replace(prefix, "buttons/");
            assets[lookupName] = sprite;
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

    public static T Get<T>(string name) => (T)assets[name];
}