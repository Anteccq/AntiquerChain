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
    public class NetworkManager : IDisposable
    {
        private Server _server;
        private ILogger _logger = Logging.Create<NetworkManager>();

        public NetworkManager(CancellationToken token)
        {
            var tokenSource = new CancellationTokenSource();
            _server = new Server(tokenSource);
            _server.NewConnection += NewConnection;
            _server.MessageReceived += MessageHandle;
            token.Register(_server.Dispose);
        }

        public async Task StartServerAsync() => await (_server?.StartAsync() ?? Task.CompletedTask);

        async Task NewConnection(IPEndPoint ipEndPoint)
        {
            Console.WriteLine($"{ipEndPoint}");
            using var client = new TcpClient(AddressFamily.InterNetwork);
            await client.ConnectAsync(ipEndPoint.Address, Server.SERVER_PORT);
            await using var stream = client.GetStream();
            var d =JsonSerializer.Serialize(HandShake.CreateMessage(_server.ConnectingEndPoints));
            await stream.WriteAsync(d, 0, d.Length);
        }

        Task MessageHandle(Message msg, IPEndPoint endPoint)
        {
            _logger.LogInformation($"Message has arrived from {endPoint}");
            return msg.Type switch
            {
                MessageType.HandShake => HandShakeHandle(JsonSerializer.Deserialize<HandShake>(msg.Payload), endPoint),
                MessageType.Addr => AddrHandle(JsonSerializer.Deserialize<AddrPayload>(msg.Payload), endPoint),
                MessageType.Inventory => Task.CompletedTask,
                MessageType.Notice => Task.CompletedTask,
                MessageType.Ping => Task.CompletedTask,
                _ => Task.CompletedTask
            };
        }

        async Task HandShakeHandle(HandShake msg, IPEndPoint endPoint)
        {
            if(!CompareIpEndPoints(_server.ConnectingEndPoints, msg.KnownIpEndPoints)) return;
            _server.ConnectingEndPoints = _server.ConnectingEndPoints.Union(msg.KnownIpEndPoints) as List<IPEndPoint>;
            await BroadcastEndPointsAsync();
        }

        async Task AddrHandle(AddrPayload msg, IPEndPoint endPoint)
        {
            if (!CompareIpEndPoints(_server.ConnectingEndPoints, msg.KnownIpEndPoints)) return;
            _server.ConnectingEndPoints = _server.ConnectingEndPoints.Union(msg.KnownIpEndPoints) as List<IPEndPoint>;
            await BroadcastEndPointsAsync();
        }

        async Task BroadcastEndPointsAsync()
        {
            _logger.LogInformation("Broadcast EndPoints...");
            if (_server.ConnectingEndPoints is null) return;
            var addrMsg = AddrPayload.CreateMessage(_server.ConnectingEndPoints);
            foreach (var ep in _server.ConnectingEndPoints)
            {
                await SendMessageAsync(ep, addrMsg);
            }
        }

        public async Task ConnectAsync(IPEndPoint endPoint) =>
            await SendMessageAsync(endPoint, HandShake.CreateMessage(_server.ConnectingEndPoints));

        static async Task SendMessageAsync(IPEndPoint endPoint, Message message)
        {
            using var client = new TcpClient();
            try
            {
                await client.ConnectAsync(endPoint.Address, Server.SERVER_PORT);
                await using var stream = client.GetStream();
                await JsonSerializer.SerializeAsync(stream, message);
            }
            finally
            {
                //log
            }
        }

        static bool CompareIpEndPoints(IEnumerable<IPEndPoint> listA, IEnumerable<IPEndPoint> listB)
        {
            var listAstr = listA.Select(i => $"{i.Address}").OrderBy(s => s).ToArray();
            var listBstr = listB.Select(i => $"{i.Address}").OrderBy(s => s).ToArray();
            return listBstr.SequenceEqual(listAstr);
        }

        public void Dispose() => _server.Dispose();
    }
}