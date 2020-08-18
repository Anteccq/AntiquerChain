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
        private ILogger _logger = Logging.Create<Server>();
        private TcpListener _listener;
        public CancellationTokenSource TokenSource { get; set; }
        public CancellationToken Token { get; }
        private Task _listenTask;

        public event Func<Message, IPEndPoint, Task> MessageReceived;
        public event Func<IPEndPoint, MessageType, Task> NewConnection;

        public Server( CancellationTokenSource tokenSource)
        {
            _logger.LogInformation("Server: Server Initialization.");
            TokenSource = tokenSource;
            Token = tokenSource.Token;
        }

        public async Task StartAsync()
        {
            _logger.LogInformation("Server: Start Listening...");
            var endPoint = IPEndPoint.Parse($"127.0.0.1:{NetworkConstant.SERVER_PORT}");
            _listener = new TcpListener(endPoint);
            _listener.Start();
            await (NewConnection?.Invoke(endPoint, MessageType.HandShake) ?? Task.CompletedTask);
            _listenTask = ConnectionWaitAsync();
        }

        async Task ConnectionWaitAsync()
        {
            _logger.LogInformation("Server: Waiting for connection");
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
                        await (NewConnection?.Invoke(endPoint as IPEndPoint, message.Type) ?? Task.CompletedTask);
                        await (MessageReceived?.Invoke(message, endPoint as IPEndPoint) ?? Task.CompletedTask);
                    }
                    catch (SocketException e)
                    {
                        _logger.LogError("Server: Error on ConnectionWaiting.", e);
                    }
                }
            }
            _listener.Stop();
        }

        public void Dispose()
        {
            _logger.LogInformation("Server: Stop listening...");
            if (TokenSource is null) return;
            TokenSource.Cancel();
            TokenSource.Dispose();
            TokenSource = null;
        }
    }
}
