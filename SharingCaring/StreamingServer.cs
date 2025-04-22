using System.Net;
using System.Net.Sockets;
using System.Text;
using SharingCaring.Util;

namespace SharingCaring;

public class StreamingServer(Queue<byte[]> queue)
{
    public async Task RunAsync(CancellationToken cancellationToken)
    {
        var tcpListener = new TcpListener(IPAddress.Any, 8000);

        tcpListener.Start();

        while (!cancellationToken.IsCancellationRequested)
        {
            var tcpClient = await tcpListener.AcceptTcpClientAsync(cancellationToken);

            try
            {
                await HandleConnection(cancellationToken, tcpClient);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }
    }

    private static async Task HandleConnection(CancellationToken cancellationToken, TcpClient tcpClient)
    {
        var receiveBuffer = new byte[1024];
        var stream = tcpClient.GetStream();

        _ = await stream.ReadAsync(receiveBuffer, cancellationToken);

        var path = HttpUtils.GetRoute(receiveBuffer);

        if (path.SequenceEqual("/api/downstream"u8))
        {
            var requestInitialHeader = """
                                       HTTP/1.1 206 PARTIAL-CONTENT
                                       Access-Control-Allow-Headers: *
                                       Access-Control-Allow-Origin: *
                                       Connection: keep-alive
                                       Content-Type: video/mp4
                                       
                                       
                                       """u8;
            

            stream.Write(requestInitialHeader);

            // var videoBytes = queue.TryDequeue(out var data) ? data : [];
            var video = File.ReadAllBytes("./mov_bbb.mp4");

            while (!cancellationToken.IsCancellationRequested)
            {
                stream.Write(video);
                stream.Flush();
                Thread.Sleep(500);
            }
        }

        await tcpClient.Client.DisconnectAsync(true, cancellationToken);
    }
}