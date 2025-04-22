using System.Runtime.CompilerServices;

namespace SharingCaring.Util;

public static class HttpUtils
{
    private const byte SpaceByte = (byte)' ';

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ReadOnlySpan<byte> GetRoute(ReadOnlySpan<byte> request)
    {
        var pathStart = request.IndexOf(SpaceByte) + 1;
        var pathEnd = pathStart + request[pathStart..].IndexOf(SpaceByte);

        return request.Slice(pathStart, pathEnd - pathStart);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ReadOnlySpan<byte> GetHeader(ReadOnlySpan<byte> request, ReadOnlySpan<byte> header)
    {
        var headerStart = request.IndexOf(header);

        if (headerStart == -1)
        {
            return [];
        }

        var valueStart = headerStart + header.Length + 2; // +2 for ': '
        var valueLength = request[valueStart..].IndexOf((byte)'\n');

        return request.Slice(valueStart, valueLength);
    }
}