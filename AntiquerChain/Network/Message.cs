using System;
using System.Collections.Generic;
using System.Text;

namespace AntiquerChain.Network
{
    public class Message
    {
        public MessageType Type { get; set; }

        public byte[] Payload { get; set; }
    }
}
