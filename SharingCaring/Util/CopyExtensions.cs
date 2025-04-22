using System.Runtime.CompilerServices;

namespace SharingCaring.Util;

public static class CopyExtensions
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int AddAt(this Span<byte> dest, int startingAt, ReadOnlySpan<byte> src)
    {
        for (var i = 0; i < src.Length; i++)
        {
            dest[i + startingAt] = src[i];
        }

        return src.Length;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int AddAt(this Span<byte> dest, int startingAt, byte src)
    {
        dest[startingAt] = src;

        return 1;
    }
}