using System;
using System.Collections.Generic;
using System.Net;
using AntiquerChain.Blockchain;
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

    public class Ping
    {
        public static Message CreateMessage()
        {
            return new Message()
            {
                Type = MessageType.Ping,
                Payload = new byte[] {0}
            };
        }
    }

    public class SurfaceHandShake
    {
        public static Message CreateMessage()
        {
            return new Message()
            {
                Type = MessageType.SurfaceHandShake,
                Payload = new byte[] {0}
            };
        }
    }

    public class NewTransaction
    {
        public Transaction Transaction { get; set; }

        public static Message CreateMessage(Transaction transaction)
        {
            return new Message()
            {
                Type = MessageType.NewTransaction,
                Payload = JsonSerializer.Serialize(new NewTransaction()
                {
                    Transaction = transaction
                })
            };
        }
    }

    public class NewBlock
    {
        public Block Block { get; set; }

        public static Message CreateMessage(Block block)
        {
            return new Message()
            {
                Type = MessageType.NewBlock,
                Payload = JsonSerializer.Serialize(new NewBlock()
                {
                    Block = block
                })
            };
        }
    }
}
