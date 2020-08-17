using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices.ComTypes;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Utf8Json;

namespace AntiquerChain.Network
{
    public class Surface
    {
        public const int SURFACE_PORT = 50152;
        private ILogger _logger = Logging.Create<Surface>();
        private TcpListener _listener;
        public CancellationTokenSource TokenSource { get; set; }
        public CancellationToken Token { get; }
        private Task _listenTask;

        public event Func<Message, Task> MessageReceived;

        public Surface(CancellationTokenSource cts)
        {
            _logger.LogInformation("Surface Initializing...");
            TokenSource = cts;
            Token = cts.Token;
        }

        public async Task StartAsync()
        {
            _logger.LogInformation("Start Listening...");
            var endPoint = IPEndPoint.Parse($"127.0.0.1:{SURFACE_PORT}");
            _listener = new TcpListener(endPoint);
            _listener.Start();
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
                    if ((await Task.WhenAny(t, tcs.Task)).IsCanceled) break;
                    try
                    {
                        using var client = t.Result;
                        var message = await JsonSerializer.DeserializeAsync<Message>(client.GetStream());
                        var endPoint = client.Client.RemoteEndPoint;
                        await (MessageReceived?.Invoke(message) ?? Task.CompletedTask);
                    }
                    catch (SocketException e)
                    {
                        _logger.LogError("Error on ConnectionWaiting.", e);
                    }
                }
            }
            _listener.Stop();
        }
    }
}
