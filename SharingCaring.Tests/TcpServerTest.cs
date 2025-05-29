using System.Collections.Concurrent;
using System.Text;
using JetBrains.Annotations;

namespace SharingCaring.Tests;

[TestSubject(typeof(TcpServer))]
public class TcpServerTest
{
    [Fact]
    public async Task Verify_Payload_Sequence()
    {
        const string message = "Hey there!";
        var queue = new ConcurrentQueue<byte[]>();

        var tcpServerTask =
            Task.Run(() => new TcpServer(queue).RunAsync(CancellationToken.None).GetAwaiter().GetResult());
        var streamingServerTask =
            Task.Run(() => new StreamingServer(queue).RunAsync(CancellationToken.None).GetAwaiter().GetResult());

        while (tcpServerTask.Status < TaskStatus.WaitingToRun || streamingServerTask.Status < TaskStatus.WaitingToRun)
        {
            await Task.Yield();
        }

        var sync = new CountdownEvent(1);
        var clientTask = Task.Run(async () =>
        {
            using var client = new HttpClient();
            client.BaseAddress = new Uri($"http://127.0.0.1:{TcpServer.Port}");
            var content = new StringContent(message, Encoding.UTF8, "text/plain");
            await client.PostAsync("/api/upstream", content);

            sync.Signal();
        });

        var testTask = Task.Run(async () =>
        {
            sync.Wait(TimeSpan.FromSeconds(5));

            using var client = new HttpClient();
            client.BaseAddress = new Uri($"http://127.0.0.1:{StreamingServer.Port}");
            var res = await client.GetAsync("/api/downstream");

            var result = await res.Content.ReadAsStringAsync();

            Assert.Equal(message, result);
        });

        await Task.WhenAll(clientTask, testTask);
    }
}