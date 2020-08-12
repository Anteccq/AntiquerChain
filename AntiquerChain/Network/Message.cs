using System;
using System.Collections.Generic;
using System.Net;
using Utf8Json;

namespace AntiquerChain.Network
{
    public class Message
    {
        public MessageType Type { get; set; }

        public byte[] Payload { get; set; }
    }

    public class HandShake
    {
        public List<IPEndPoint> KnownIpEndPoints { get; set; }

        public static Message CreateMessage(List<IPEndPoint> ipEndPoints)
        {
            return new Message()
            {
                Type = MessageType.HandShake,
                Payload = JsonSerializer.Serialize(new HandShake() {KnownIpEndPoints = ipEndPoints})
            };
        }
    }

    public class AddrPayload
    {
        public List<IPEndPoint> KnownIpEndPoints { get; set; }

        public static Message CreateMessage(List<IPEndPoint> ipEndPoints)
        {
            return new Message()
            {
                Type = MessageType.Addr,
                Payload = JsonSerializer.Serialize(new AddrPayload() { KnownIpEndPoints = ipEndPoints })
            };
        }
    }
}
