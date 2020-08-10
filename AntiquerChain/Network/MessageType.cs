using System;
using System.Collections.Generic;
using System.Text;

namespace AntiquerChain.Network
{
    public enum MessageType
    {
        HandShake,
        Ping,
        Inventory,
        Notice
    }
}
