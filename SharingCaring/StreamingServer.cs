using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using SharingCaring.Util;

namespace SharingCaring;

public class StreamingServer(ConcurrentQueue<byte[]> queue)
{
    public const int Port = 8000;

    public async Task RunAsync(CancellationToken cancellationToken)
    {
        var tcpListener = new TcpListener(IPAddress.Any, Port);

        tcpListener.Start();

        while (!cancellationToken.IsCancellationRequested)
        {
            var tcpClient = await tcpListener.AcceptTcpClientAsync(cancellationToken);

            try
            {
                await HandleConnection(tcpClient, cancellationToken);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }
    }

    private async Task HandleConnection(TcpClient tcpClient, CancellationToken cancellationToken)
    {
        var receiveBuffer = new byte[1024];
        var stream = tcpClient.GetStream();

        _ = await stream.ReadAsync(receiveBuffer, cancellationToken);

        var path = HttpUtils.GetRoute(receiveBuffer);

        if (path.SequenceEqual("/api/downstream"u8))
        {
            var requestInitialHeader = """
                                       HTTP/1.1 200 OK
                                       Access-Control-Allow-Headers: *
                                       Access-Control-Allow-Origin: *
                                       Connection: keep-alive
                                       Content-Type: video/mp4
                                       
                                       
                                       """u8;
            

            stream.Write(requestInitialHeader);

            if (queue.TryDequeue(out var data))
            {
                stream.Write(data);
                await stream.FlushAsync(cancellationToken);
            }
        }

        await tcpClient.Client.DisconnectAsync(true, cancellationToken);
    }
}