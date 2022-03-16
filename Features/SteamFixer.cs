using System;
using System.IO;
using AmadarePlugin.Options;
using BepInEx;

namespace AmadarePlugin.Features;

public class SteamFixer
{
    public static void Run()
    {
        if (PublishPlatform.PlatformName == "steam" && OptionsManager.AddSteamAppId)
        {
            var appidPath = Path.Combine(
                Path.GetDirectoryName(Paths.ExecutablePath),
                "steam_appid.txt"
            );
            if (!File.Exists(appidPath))
            {
                try
                {
                    File.WriteAllText(appidPath, "527230");
                    Plugin.Log.LogInfo("Added steam_appid.txt file");
                }
                catch (Exception ex)
                {
                    Plugin.Log.LogError($"Error on creating steam_appid.txt file: {ex.Message}");
                }
            }
        }
    }
}