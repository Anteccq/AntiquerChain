using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Utf8Json;

namespace AntiquerChain.Network
{
    public class NetworkManager
    {
        private Server _server;

        public NetworkManager()
        {
            var tokenSource = new CancellationTokenSource();
            _server = new Server(tokenSource);
            _server.NewConnection += NewConnection;
            _server.MessageReceived += MessageHandle;
        }

        void NewConnection(IPEndPoint ipEndPoint)
        {
            using var client = new TcpClient(AddressFamily.InterNetwork);
            client.ConnectAsync(ipEndPoint.Address, Server.SERVER_PORT);
            using var stream = client.GetStream();
            JsonSerializer.SerializeAsync(stream, HandShake.CreateMessage(_server.ConnectingEndPoints));
        }

        Task MessageHandle(Message msg, IPEndPoint endPoint)
        {
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
    }
}