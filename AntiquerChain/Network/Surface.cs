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
    public class Surface : IDisposable
    {
        private ILogger _logger = Logging.Create<Surface>();
        private TcpListener _listener;
        public CancellationTokenSource TokenSource { get; set; }
        public CancellationToken Token { get; }
        private Task _listenTask;

        public event Func<Message, Task> MessageReceived;

        public Surface(CancellationTokenSource cts)
        {
            _logger.LogInformation("Surface: Surface Initialization");
            TokenSource = cts;
            Token = cts.Token;
        }

        public void Start()
        {
            _logger.LogInformation("Surface: Start Listening...");
            var endPoint = IPEndPoint.Parse($"127.0.0.1:{NetworkConstant.SURFACE_PORT}");
            _listener = new TcpListener(endPoint);
            _listener.Start();
            _listenTask = ConnectionWaitAsync();
        }

        async Task ConnectionWaitAsync()
        {
            _logger.LogInformation("Surface: Waiting for Connection");
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
                        _logger.LogError("Surface: Error on ConnectionWaiting.", e);
                    }
                }
            }
            _listener.Stop();
        }

        public void Dispose()
        {
            _logger.LogInformation("Surface: Stop listening...");
            if (TokenSource is null) return;
            TokenSource.Cancel();
            TokenSource.Dispose();
            TokenSource = null;
        }
    }
}
