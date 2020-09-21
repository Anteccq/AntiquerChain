using System;
using System.Collections.Generic;
using System.Text;

namespace AntiquerChain.Network
{
    public enum MessageType : byte
    {
        #region Server
        
        HandShake = 0x00,
        Addr = 0x01,
        
        #endregion

        #region Surface
        
        SurfaceHandShake = 0x10,

        #endregion

        #region Transactions

        Inventory = 0x20,
        NewTransaction = 0x21,
        NewBlock = 0x22,
        RequestFullChain = 0x23,
        FullChain = 0x24,

        #endregion

        #region Others

        Ping = 0x30,
        Notice = 0x31,

        #endregion

    }
}
