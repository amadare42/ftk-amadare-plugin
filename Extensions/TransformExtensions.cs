using UnityEngine;

namespace AmadarePlugin.Extensions;

public static class TransformExtensions
{
    public static T Scale1<T>(this T transform) where T : Transform
    {
        transform.localScale = Vector3.one;
        return transform;
    }

    public static float ResolutionFactorX => Screen.width / 3840f;
    public static float ResolutionFactorY => Screen.height / 2160f;

    public static RectTransform ScaleResolutionBased(this RectTransform transform, float scaleOn4k = 1)
    {
        transform.localScale = new Vector3(scaleOn4k * ResolutionFactorX, scaleOn4k * ResolutionFactorY, 1);
        return transform;
    }
    public static RectTransform Scale(this RectTransform transform, float scale)
    {
        transform.localScale = new Vector3(scale, scale, scale);
        return transform;
    }
}