using System.Collections;

namespace AmadarePlugin.Extensions;

public static class EnumerableExtensions
{
    public static IEnumerable ToIEnumerable(this IEnumerator enumerator) {
        while ( enumerator.MoveNext() ) {
            yield return enumerator.Current;
        }
    }
}