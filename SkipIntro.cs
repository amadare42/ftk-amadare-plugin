using AmadarePlugin.Options;

namespace AmadarePlugin;

public class SkipIntro
{
    public SkipIntro()
    {
        if (!OptionsManager.SkipIntro) return;
        
        // emulates pressing button to skip intro on every frame
        On.SplashScreen.GetAnyButton += (_, _) => true;
        
        // prevents "prepare to die" message
        uiStartGame.gIsFirstTime = false;
    }
}