using System.Text;

namespace AmadarePlugin.Extensions;

public static class StringBuilderExtensions
{
    public static StringBuilder Clear(this StringBuilder sb)
    {
        // clear for .net 3.5
        sb.Length = 0;
        return sb;
    }
}