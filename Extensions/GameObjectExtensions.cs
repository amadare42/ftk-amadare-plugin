using UnityEngine;

namespace AmadarePlugin.Extensions;

public static class GameObjectExtensions
{
    public static bool IsOk(this Object go)
    {
        return go != null && go;
    }
}