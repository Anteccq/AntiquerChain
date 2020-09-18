using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using AntiquerChain.Blockchain;
using Microsoft.Extensions.Logging;
using Utf8Json;
using static AntiquerChain.Network.Util.Messenger;

namespace AntiquerChain.Network
{
    public class NetworkManager : IDisposable
    {
        private Server _server;
        private ILogger _logger = Logging.Create<NetworkManager>();
        private Timer _timer;
        private List<IPEndPoint> ConnectSurfaces { get; } = new List<IPEndPoint>();
        private List<IPEndPoint> ConnectServers { get; set; } = new List<IPEndPoint>();

        public NetworkManager(CancellationToken token)
        {
            var tokenSource = new CancellationTokenSource();
            _server = new Server(tokenSource);
            _server.NewConnection += NewConnection;
            _server.MessageReceived += MessageHandle;
            _timer = new Timer(async _ => await AllConnectionCheckAsync(), null,
                TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(10));
            token.Register(Dispose);
        }

        public async Task StartServerAsync() => await (_server?.StartAsync() ?? Task.CompletedTask);

        async Task NewConnection(IPEndPoint ipEndPoint, MessageType type)
        {
            var list = type switch
            {
                MessageType.HandShake => ConnectServers,
                MessageType.SurfaceHandShake => ConnectSurfaces,
                _ => null
            };
            if(list is null) return;
            lock (list)
            {
                if (list.Any(x => Equals(x.Address, ipEndPoint.Address))) return;
                list.Add(ipEndPoint);
            }
            _logger.LogInformation($"Server: New Connection from {ipEndPoint} > {type}");
            if(type != MessageType.HandShake) return;
            try
            {
                await SendMessageAsync(ipEndPoint.Address, NetworkConstant.SERVER_PORT, HandShake.CreateMessage(ConnectServers));
            }
            catch (SocketException)
            {
                RemoveEndPoint(ipEndPoint, list);
                await BroadcastEndPointsAsync();
            }
        }

        Task MessageHandle(Message msg, IPEndPoint endPoint)
        {
            _logger.LogInformation($"Server: Message has arrived from {endPoint} : MSG_TYPE: {msg.Type} : {DateTime.Now:ss.FFFF}");
            return msg.Type switch
            {
                MessageType.HandShake => HandShakeHandle(JsonSerializer.Deserialize<HandShake>(msg.Payload), endPoint),
                MessageType.Addr => AddrHandle(JsonSerializer.Deserialize<AddrPayload>(msg.Payload), endPoint),
                MessageType.Inventory => Task.CompletedTask,
                MessageType.NewTransaction => NewTransactionHandle(JsonSerializer.Deserialize<NewTransaction>(msg.Payload), endPoint),
                MessageType.Notice => Task.CompletedTask,
                MessageType.Ping => Task.CompletedTask,
                MessageType.SurfaceHandShake => SurfaceHandShakeHandle(endPoint),
                _ => Task.CompletedTask
            };
        }

        async Task HandShakeHandle(HandShake msg, IPEndPoint endPoint)
        {
            if(CompareIpEndPoints(ConnectServers, msg.KnownIpEndPoints)) return;
            lock (ConnectServers)
            {
                ConnectServers = UnionEndpoints(ConnectServers, msg.KnownIpEndPoints);
            }
            await BroadcastEndPointsAsync();
        }

        async Task AddrHandle(AddrPayload msg, IPEndPoint endPoint)
        {
            if (CompareIpEndPoints(ConnectServers, msg.KnownIpEndPoints)) return;
            lock (ConnectServers)
            {
                ConnectServers = UnionEndpoints(ConnectServers, msg.KnownIpEndPoints);
            }
            await BroadcastEndPointsAsync();
        }

        async Task NewTransactionHandle(NewTransaction msg, IPEndPoint endPoint)
        {
            _logger.LogInformation($"New Transaction : {msg.Transaction.Id}");
            BlockchainManager.TransactionPool.Add(msg.Transaction);
        }

        async Task NewBlockHandle(NewBlock msg, IPEndPoint endPoint)
        {
            _logger.LogInformation($"New Block : {msg.Block.Id.String}");

        }

        async Task SurfaceHandShakeHandle(IPEndPoint endPoint)
        {
            _logger.LogInformation($"Server: Surface is connected from {endPoint}");
        }

        async Task BroadcastEndPointsAsync()
        {
            if (ConnectServers is null) return;
            _logger.LogInformation($"Server: Broadcast EndPoints to {ConnectServers.Count}");
            var addrMsg = AddrPayload.CreateMessage(ConnectServers);
            var disconnectedList = new List<IPEndPoint>();
            foreach (var ep in ConnectServers)
            {
                try { await SendMessageAsync(ep.Address, NetworkConstant.SERVER_PORT, addrMsg); }
                catch(SocketException)
                {
                    disconnectedList.Add(ep);
                }
            }
            if(disconnectedList.Count == 0) return;
            foreach (var ep in disconnectedList) RemoveEndPoint(ep, ConnectServers);
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
                await SendMessageAsync(endPoint.Address, NetworkConstant.SERVER_PORT, HandShake.CreateMessage(ConnectServers));
            }
            catch (SocketException)
            {
                _logger.LogInformation($"Server: {endPoint} > No response");
            }
        }

        async Task AllConnectionCheckAsync()
        {
            _logger.LogInformation("Server: Servers Connection Checking...");
            await AllServerConnectionCheckAsync();
            _logger.LogInformation("Server: Surfaces Connection Checking...");
            await AllSurfaceConnectionCheckAsync();
        }

        async Task AllServerConnectionCheckAsync()
        {
            if (ConnectServers.Count == 0) return;
            var msg = Ping.CreateMessage();
            var disconnectedList = new List<IPEndPoint>();
            foreach (var ep in ConnectServers)
            {
                try { await SendMessageAsync(ep.Address, NetworkConstant.SERVER_PORT, msg); }
                catch (SocketException)
                {
                    disconnectedList.Add(ep);
                }
            }
            if (disconnectedList.Count == 0) return;
            foreach (var ep in disconnectedList) RemoveEndPoint(ep, ConnectServers);
            await BroadcastEndPointsAsync();
        }

        async Task AllSurfaceConnectionCheckAsync()
        {
            if (ConnectSurfaces.Count == 0) return;
            var msg = Ping.CreateMessage();
            var disconnectedList = new List<IPEndPoint>();
            foreach (var ep in ConnectSurfaces)
            {
                try { await SendMessageAsync(ep.Address, NetworkConstant.SURFACE_PORT, msg); }
                catch (SocketException)
                {
                    disconnectedList.Add(ep);
                }
            }
            if (disconnectedList.Count == 0) return;
            foreach (var ep in disconnectedList) RemoveEndPoint(ep, ConnectSurfaces);
        }

        private void RemoveEndPoint(IPEndPoint endPoint, List<IPEndPoint> list)
        {
            var peers = list;
            lock (peers)
            {
                var index = peers.FindIndex(peer => Equals(peer.Address, endPoint.Address));
                if(index < 0) return;
                peers.RemoveAt(index);
            }
            _logger.LogInformation($"Server: Disconnected > {endPoint.Address}");
        }

        static bool CompareIpEndPoints(IEnumerable<IPEndPoint> listA, IEnumerable<IPEndPoint> listB)
        {
            var listAstr = listA.DistinctByAddress().Select(i => $"{i.Address}").OrderBy(s => s).ToArray();
            var listBstr = listB.DistinctByAddress().Select(i => $"{i.Address}").OrderBy(s => s).ToArray();
            return listBstr.SequenceEqual(listAstr);
        }

        public void Dispose()
        {
            _timer.Dispose();
            _server.Dispose();
            ConnectSurfaces?.Clear();
            ConnectServers?.Clear();
        }
    }
}