using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Utf8Json;

namespace AntiquerChain.Network
{
    public class Server : IDisposable
    {
        private TcpListener _listener;
        public CancellationTokenSource TokenSource { get; set; }
        public CancellationToken Token { get; }
        private Task _listenTask;

        public event Action<Message> MessageReceived;
        public event Action<IPEndPoint> NewConnection;
        public List<IPEndPoint> ConnectingEndPoints { get; set; } = new List<IPEndPoint>();

        public Server( CancellationTokenSource tokenSource)
        {
            TokenSource = tokenSource;
            Token = tokenSource.Token;
        }

        public void Start()
        {
            var endPoint = IPEndPoint.Parse("0.0.0.0:50151");
            _listener = new TcpListener(endPoint);
            _listener.Start();
            AddEndPoints(_listener.LocalEndpoint);
            _listenTask = ConnectionWaitAsync();
        }

        async Task ConnectionWaitAsync()
        {
            Console.WriteLine("接続待機");
            if(_listener is null) return;

            while (!Token.IsCancellationRequested)
            {
                var t = _listener.AcceptTcpClientAsync();
                if ( t.IsCanceled ) break;
                try
                {
                    using var client = t.Result;
                    var message = await JsonSerializer.DeserializeAsync<Message>(client.GetStream());
                    MessageReceived?.Invoke(message);
                    AddEndPoints(client.Client.RemoteEndPoint);
                }
                catch (SocketException)
                {
                    //Log
                }
            }
            _listener.Stop();
        }

        public void AddEndPoints(EndPoint endPoint)
        {
            if(!(endPoint is IPEndPoint ipEndPoint)) return;
            lock (ConnectingEndPoints)
            {
                if(ConnectingEndPoints.Contains(ipEndPoint)) return;
                ConnectingEndPoints.Add(ipEndPoint);
            }
        }

        public void Dispose()
        {
            if(TokenSource is null) return;
            TokenSource.Cancel();
            TokenSource.Dispose();
            TokenSource = null;
        }
    }
}
