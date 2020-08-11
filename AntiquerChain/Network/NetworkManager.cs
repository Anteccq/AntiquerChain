using System;
using System.Collections.Generic;
using System.Net;
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
            _server.NewConnection += NewConnection;
        }

        public void NewConnection(IPEndPoint ipEndPoint)
        {
            
        }
    }
}