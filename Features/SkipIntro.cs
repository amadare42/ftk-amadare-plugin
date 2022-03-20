using AmadarePlugin.Options;

namespace AmadarePlugin.Features;

public static class SkipIntro
{
    public static void Init()
    {
        if (!OptionsManager.SkipIntro) return;
        
        // emulates pressing button to skip intro on every frame
        On.SplashScreen.GetAnyButton += (_, _) => true;
        
        // prevents "prepare to die" message
        uiStartGame.gIsFirstTime = false;
    }
}