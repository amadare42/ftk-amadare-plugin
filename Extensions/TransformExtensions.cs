using UnityEngine;

namespace AmadarePlugin.Extensions;

public static class TransformExtensions
{
    public static T Scale1<T>(this T transform) where T : Transform
    {
        transform.localScale = Vector3.one;
        return transform;
    }
}