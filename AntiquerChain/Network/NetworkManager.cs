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
    public class NetworkManager
    {
        private Server _server;
        private ILogger _logger = Logging.Create<NetworkManager>();
        private Timer _timer;

        public NetworkManager(CancellationToken token)
        {
            var tokenSource = new CancellationTokenSource();
            _server = new Server(tokenSource);
            _server.NewConnection += NewConnection;
            _server.MessageReceived += MessageHandle;
            _timer = new Timer(async _ => await AllConnectionCheckAsync(), null,
                TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(10));
            token.Register(_timer.Dispose);
            token.Register(_server.Dispose);
        }

        public async Task StartServerAsync() => await (_server?.StartAsync() ?? Task.CompletedTask);

        async Task NewConnection(IPEndPoint ipEndPoint)
        {
            Console.WriteLine($"{ipEndPoint}");
            try
            {
                await SendMessageAsync(ipEndPoint, HandShake.CreateMessage(_server.ConnectingEndPoints));
            }
            catch (SocketException)
            {
                RemoveEndPoint(ipEndPoint);
                await BroadcastEndPointsAsync();
            }
        }

        Task MessageHandle(Message msg, IPEndPoint endPoint)
        {
            _logger.LogInformation($"Message has arrived from {endPoint} : MSG_TYPE: {msg.Type} : {DateTime.Now:ss.FFFF}");
            return msg.Type switch
            {
                MessageType.HandShake => HandShakeHandle(JsonSerializer.Deserialize<HandShake>(msg.Payload), endPoint),
                MessageType.Addr => AddrHandle(JsonSerializer.Deserialize<AddrPayload>(msg.Payload), endPoint),
                MessageType.Inventory => Task.CompletedTask,
                MessageType.Notice => Task.CompletedTask,
                MessageType.Ping => Task.CompletedTask,
                MessageType.SurfaceHandShake => SurfaceHandShakeHandle(endPoint),
                _ => Task.CompletedTask
            };
        }

        async Task HandShakeHandle(HandShake msg, IPEndPoint endPoint)
        {
            if(CompareIpEndPoints(_server.ConnectingEndPoints, msg.KnownIpEndPoints)) return;
            lock (_server.ConnectingEndPoints)
            {
                _server.ConnectingEndPoints = UnionEndpoints(_server.ConnectingEndPoints, msg.KnownIpEndPoints);
            }
            await BroadcastEndPointsAsync();
        }

        async Task AddrHandle(AddrPayload msg, IPEndPoint endPoint)
        {
            if (CompareIpEndPoints(_server.ConnectingEndPoints, msg.KnownIpEndPoints)) return;
            lock (_server.ConnectingEndPoints)
            {
                _server.ConnectingEndPoints = UnionEndpoints(_server.ConnectingEndPoints, msg.KnownIpEndPoints);
            }
            await BroadcastEndPointsAsync();
        }

        async Task SurfaceHandShakeHandle(IPEndPoint endPoint)
        {
            _logger.LogInformation($"Surface is connected from {endPoint}");
        }

        async Task BroadcastEndPointsAsync()
        {
            if (_server.ConnectingEndPoints is null) return;
            _logger.LogInformation($"Broadcast EndPoints to {_server.ConnectingEndPoints.Count}");
            var addrMsg = AddrPayload.CreateMessage(_server.ConnectingEndPoints);
            var disconnectedList = new List<IPEndPoint>();
            foreach (var ep in _server.ConnectingEndPoints)
            {
                try { await SendMessageAsync(ep, addrMsg); }
                catch(SocketException)
                {
                    disconnectedList.Add(ep);
                }
            }
            if(disconnectedList.Count == 0) return;
            foreach (var ep in disconnectedList) RemoveEndPoint(ep);
            await BroadcastEndPointsAsync();
        }

        static List<IPEndPoint> UnionEndpoints(IEnumerable<IPEndPoint> listA, IEnumerable<IPEndPoint> listB)
        {
            return listA.Union(listB).DistinctByAddress().ToList();
        }

        public async Task ConnectAsync(IPEndPoint endPoint)
        {
            try
            {
                await SendMessageAsync(endPoint, HandShake.CreateMessage(_server.ConnectingEndPoints));
            }
            catch (SocketException)
            {
                _logger.LogInformation($"{endPoint}: No response");
            }
        }

        async Task AllConnectionCheckAsync()
        {
            if(_server.ConnectingEndPoints.Count == 0) return;
            var msg = Ping.CreateMessage();
            var disconnectedList = new List<IPEndPoint>();
            foreach (var ep in _server.ConnectingEndPoints)
            {
                try { await SendMessageAsync(ep, msg); }
                catch (SocketException)
                {
                    disconnectedList.Add(ep);
                }
            }
            if (disconnectedList.Count == 0) return;
            foreach (var ep in disconnectedList) RemoveEndPoint(ep);
            await BroadcastEndPointsAsync();
        }
        
        async Task SendMessageAsync(IPEndPoint endPoint, Message message)
        {
            using var client = new TcpClient();
            await client.ConnectAsync(endPoint.Address, Server.SERVER_PORT);
            await using var stream = client.GetStream();
            await JsonSerializer.SerializeAsync(stream, message);
        }

        private void RemoveEndPoint(IPEndPoint endPoint)
        {
            var peers = _server.ConnectingEndPoints;
            lock (peers)
            {
                var index = peers.FindIndex(peer => Equals(peer.Address, endPoint.Address));
                if(index < 0) return;
                peers.RemoveAt(index);
            }
            _logger.LogInformation($"Disconnected : {endPoint.Address}");
        }

        static bool CompareIpEndPoints(IEnumerable<IPEndPoint> listA, IEnumerable<IPEndPoint> listB)
        {
            var listAstr = listA.DistinctByAddress().Select(i => $"{i.Address}").OrderBy(s => s).ToArray();
            var listBstr = listB.DistinctByAddress().Select(i => $"{i.Address}").OrderBy(s => s).ToArray();
            return listBstr.SequenceEqual(listAstr);
        }
    }
}