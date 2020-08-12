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
        }

        public void NewConnection(IPEndPoint ipEndPoint)
        {
            
        }

        Task MessageHandle(Message msg, IPEndPoint endPoint)
        {
            return msg.Type switch
            {
                MessageType.HandShake => HandShakeHandle(JsonSerializer.Deserialize<HandShake>(msg.Payload), endPoint),
                MessageType.Addr => Task.CompletedTask,
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

        async Task BroadcastEndPointsAsync()
        {
            if (_server.ConnectingEndPoints is null) return;
            var addrMsg = AddrPayload.CreateMessage(_server.ConnectingEndPoints);
            foreach (var ep in _server.ConnectingEndPoints)
            {
                using var client = new TcpClient(AddressFamily.InterNetwork);
                await client.ConnectAsync(ep.Address, Server.SERVER_PORT);
                await using var stream = client.GetStream();
                await JsonSerializer.SerializeAsync(stream, addrMsg);
            }
        }

        bool CompareIpEndPoints(IEnumerable<IPEndPoint> listA, IEnumerable<IPEndPoint> listB)
        {
            var listAstr = listA.Select(i => $"{i.Address}").OrderBy(s => s).ToArray();
            var listBstr = listB.Select(i => $"{i.Address}").OrderBy(s => s).ToArray();
            return listBstr.SequenceEqual(listAstr);
        }
    }
}