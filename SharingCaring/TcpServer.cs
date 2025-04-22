using System.Buffers;
using System.Net;
using System.Net.Sockets;
using System.Text;
using SharingCaring.Util;

namespace SharingCaring;

public class TcpServer(Queue<byte[]> queue)
{
    private readonly ArrayPool<byte> _bufferPool = ArrayPool<byte>.Shared;
    private readonly byte[] _requestBuffer = new byte[1024 * 1024];
    private readonly byte[] _httpHeader = "HTTP/1.1 200 OK"u8.ToArray();

    private readonly byte[] _homeContent = File.ReadAllBytes("./Content/index.html");

    public async Task RunAsync(CancellationToken cancellationToken)
    {
        var tcpListener = new TcpListener(IPAddress.Any, 8080);

        tcpListener.Start();

        while (!cancellationToken.IsCancellationRequested)
        {
            var tpcClient = await tcpListener.AcceptTcpClientAsync(cancellationToken);

            _ = HandleRequest(tpcClient.Client, cancellationToken);
        }
    }

    private async Task HandleRequest(Socket socket, CancellationToken cancellationToken)
    {
        try
        {
            var receiveBuffer = new byte[1024 * 1024];

            var bytesRead = await socket.ReceiveAsync(receiveBuffer, cancellationToken);

            var response = GetRouteContent(receiveBuffer.AsSpan(0, bytesRead));

            await socket.SendAsync(response, cancellationToken);
            await socket.DisconnectAsync(true, cancellationToken);
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }
    }

    private ReadOnlyMemory<byte> GetRouteContent(ReadOnlySpan<byte> request)
    {
        var path = HttpUtils.GetRoute(request);

        if (path.SequenceEqual("/api/upstream"u8))
        {
            var headerEndMarker = "\r\n\r\n"u8;
            var headerEndIndex = request.IndexOf(headerEndMarker);

            if (headerEndIndex == -1) throw new Exception("Message not found");

            var messageIndex = headerEndIndex + headerEndMarker.Length;
            var message = request[messageIndex..];

            queue.Enqueue(message.ToArray());
            return GetHttpMessage([], "Content-Type: text/html"u8);
        }

        if (path.SequenceEqual("/api/downstream"u8))
        {
            var videoBytes = queue.TryDequeue(out var data) ? data : [];

            return GetHttpMessage(videoBytes, "Content-Type: video/webm;codecs=vp8"u8);
        }

        if (path.SequenceEqual("/"u8))
        {
            return GetHttpMessage(_homeContent, "Content-Type: text/html"u8);
        }

        var helloResponse = "Hello from : "u8;

        var result = new byte[helloResponse.Length + path.Length];

        helloResponse.CopyTo(result);
        path.CopyTo(result.AsSpan()[helloResponse.Length..]);

        return GetHttpMessage(result, "Content-Type: text/html"u8);
    }

    private ReadOnlyMemory<byte> GetHttpMessage(ReadOnlySpan<byte> content, ReadOnlySpan<byte> contentTypeHeader)
    {
        var contentLengthHeader = Encoding.UTF8.GetBytes($"""
                                                          Content-Length: {content.Length}


                                                          """);
        // NOTE: 2 For two \n after headers
        var totalLength = _httpHeader.Length + contentLengthHeader.Length + contentTypeHeader.Length + content.Length +
                          2;

        var buffer = new byte[totalLength];

        var buffSpan = buffer.AsSpan();
        var bytesCopied = 0;

        bytesCopied += buffSpan.AddAt(0, _httpHeader);
        bytesCopied += buffSpan.AddAt(bytesCopied, (byte)'\n');
        bytesCopied += buffSpan.AddAt(bytesCopied, contentTypeHeader);
        bytesCopied += buffSpan.AddAt(bytesCopied, (byte)'\n');
        bytesCopied += buffSpan.AddAt(bytesCopied, contentLengthHeader);
        bytesCopied += buffSpan.AddAt(bytesCopied, content);

        return buffer.AsMemory(0, bytesCopied);
    }
}