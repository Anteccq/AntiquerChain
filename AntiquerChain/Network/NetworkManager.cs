using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace AntiquerChain.Network
{
    public class NetworkManager
    {
        private Server _server;

        public NetworkManager()
        {
            var tokenSource = new CancellationTokenSource();
            _server = new Server(tokenSource);
        }
    }
}
