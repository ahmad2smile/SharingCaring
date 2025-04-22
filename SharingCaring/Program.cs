using SharingCaring;

Console.WriteLine("Hello, World!");

var queue = new Queue<byte[]>();
var server = new TcpServer(queue);
var streamingServer = new StreamingServer(queue);

var tokenSource = new CancellationTokenSource();

Console.CancelKeyPress += (_, _) => { tokenSource.Cancel(); };

await Task.WhenAll(server.RunAsync(tokenSource.Token), streamingServer.RunAsync(tokenSource.Token));