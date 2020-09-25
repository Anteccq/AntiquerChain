using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using Utf8Json;

namespace AntiquerChain.Network.Util
{
    public static class Messenger
    {
        private const int timeOut = 3000;
        public static async Task SendMessageAsync(IPAddress remoteAddress, int port, Message message)
        {
            using var client = new TcpClient(){
                SendTimeout = timeOut,
                ReceiveTimeout = timeOut
            };
            await client.ConnectAsync(remoteAddress, port);
            await using var stream = client.GetStream();
            await JsonSerializer.SerializeAsync(stream, message);
        }

        public static async Task SendMessageAsync(IPEndPoint remotePoint, Message message) =>
            await SendMessageAsync(remotePoint.Address, remotePoint.Port, message);
    }
}
