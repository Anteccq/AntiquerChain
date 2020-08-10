using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace AntiquerChain.Network.Server
{
    public class Server
    {
        private TcpListener _listener;
        public CancellationTokenSource TokenSource { get; }
        public CancellationToken Token { get; }
        private Task _listenTask;

        public Server( CancellationTokenSource tokenSource)
        {
            TokenSource = tokenSource;
            Token = tokenSource.Token;
        }

        public void StartAsync()
        {
            var endPoint = IPEndPoint.Parse("0.0.0.0:50151");
            _listener = new TcpListener(endPoint);
            _listener.Start();
            _listenTask = ConnectionWaitAsync();
        }

        async Task ConnectionWaitAsync()
        {
            Console.WriteLine("接続待機");
            if(_listener is null) return;

            var tcs = new TaskCompletionSource<int>();
            await using (Token.Register(tcs.SetCanceled))
            {
                while (!Token.IsCancellationRequested)
                {
                    var t = _listener.AcceptTcpClientAsync();
                    if ((await Task.WhenAny(t, tcs.Task)).IsCanceled) break;
                    TcpClient client;
                    try
                    {
                        client = t.Result;
                    }
                    catch (SocketException)
                    {
                        Console.WriteLine(e);
                        throw;
                    }
                }
            }
        }
    }
}
