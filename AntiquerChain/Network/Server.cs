using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Utf8Json;

namespace AntiquerChain.Network
{
    public class Server : IDisposable
    {
        public const int SERVER_PORT = 50151;
        private ILogger _logger = Logging.Create<Server>();
        private TcpListener _listener;
        public CancellationTokenSource TokenSource { get; set; }
        public CancellationToken Token { get; }
        private Task _listenTask;

        public event Func<Message, IPEndPoint, Task> MessageReceived;
        public event Func<IPEndPoint, Task> NewConnection;
        public List<IPEndPoint> ConnectingEndPoints { get; set; } = new List<IPEndPoint>();

        public Server( CancellationTokenSource tokenSource)
        {
            _logger.LogInformation("Server Initializing...");
            TokenSource = tokenSource;
            Token = tokenSource.Token;
        }

        public async Task StartAsync()
        {
            _logger.LogInformation("Start Listening...");
            var endPoint = IPEndPoint.Parse($"127.0.0.1:{SERVER_PORT}");
            _listener = new TcpListener(endPoint);
            _listener.Start();
            await AddEndPoints(_listener.LocalEndpoint);
            _listenTask = ConnectionWaitAsync();
        }

        async Task ConnectionWaitAsync()
        {
            _logger.LogInformation("Connect Waiting");
            if (_listener is null) return;
            var tcs = new TaskCompletionSource<int>();
            await using (Token.Register(tcs.SetCanceled))
            {
                while (!Token.IsCancellationRequested)
                {
                    var t = _listener.AcceptTcpClientAsync();
                    if ((await Task.WhenAny(t,tcs.Task)).IsCanceled) break;
                    try
                    {
                        using var client = t.Result;
                        var message = await JsonSerializer.DeserializeAsync<Message>(client.GetStream());
                        var endPoint = client.Client.RemoteEndPoint;
                        await (MessageReceived?.Invoke(message, endPoint as IPEndPoint) ?? Task.CompletedTask);
                        await AddEndPoints(endPoint);
                    }
                    catch (SocketException e)
                    {
                        _logger.LogError("Error on ConnectionWaiting.", e);
                    }
                }
            }
            _logger.LogInformation("ちゅうぶらりん");
            _listener.Stop();
        }

        public async Task AddEndPoints(EndPoint endPoint)
        {
            if(!(endPoint is IPEndPoint ipEndPoint)) return;
            lock (ConnectingEndPoints)
            {
                if(ConnectingEndPoints.Any(x => Equals(x.Address, ipEndPoint.Address))) return;
                ConnectingEndPoints.Add(ipEndPoint);
            }
            await (NewConnection?.Invoke(ipEndPoint) ?? Task.CompletedTask);
            _logger.LogInformation($"New Connection from {ipEndPoint}");
        }

        public void Dispose()
        {
            _logger.LogInformation("Stop listening...");
            ConnectingEndPoints?.Clear();
            _logger.LogInformation("Clear");
            if (TokenSource is null) return;
            TokenSource.Cancel();
            TokenSource.Dispose();
            TokenSource = null;
        }
    }
}
