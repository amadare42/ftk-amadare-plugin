using System;
using AmadarePlugin.Options;
using BepInEx.Logging;

namespace AmadarePlugin.Extensions;

public static class LoggingExtensions
{
    public static void DebugCond(this ManualLogSource logger, Func<object> evaluate)
    {
        if (!OptionsManager.DebugLogging) return;
        logger.LogDebug(evaluate());
    }
}