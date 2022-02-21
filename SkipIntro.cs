﻿using AmadarePlugin.Options;

namespace AmadarePlugin;

public static class SkipIntro
{
    public static void Run()
    {
        if (!OptionsManager.SkipIntro) return;
        
        // emulates pressing button to skip intro on every frame
        On.SplashScreen.GetAnyButton += (_, _) => true;
        
        // prevents "prepare to die" message
        uiStartGame.gIsFirstTime = false;
    }
}