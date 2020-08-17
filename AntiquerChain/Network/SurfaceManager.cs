using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Utf8Json;

namespace AntiquerChain.Network
{
    public class SurfaceManager
    {
        private IPEndPoint _serverEndPoint;
        private Surface _surface;
        private ILogger _logger = Logging.Create<SurfaceManager>();
        private Timer _timer;
        private readonly CancellationToken _token;

        public SurfaceManager(CancellationToken token, IPEndPoint endPoint)
        {
            _token = token;
            var tokenSource = new CancellationTokenSource();
            _surface = new Surface(tokenSource);
            _surface.MessageReceived += MessageHandle;
        }

        public void StartSurface() => _surface.Start();

        async Task ConnectionCheckAsync()
        {
            _logger.LogInformation("Connection checking..");
            try
            {
                await SendMessageAsync(_serverEndPoint, Ping.CreateMessage());
            }
            catch (SocketException)
            {
                _logger.LogInformation($"{_serverEndPoint} : Couldn't connect to the server..");
                _serverEndPoint = null;
            }
        }

        Task MessageHandle(Message msg)
        {
            _logger.LogInformation($"Message has arrived. MSG_TYPE: {msg.Type} : {DateTime.Now:ss.FFFF}");
            return Task.CompletedTask;
        }
        public async Task ConnectServerAsync(IPEndPoint serverEndPoint)
        {
            try
            {
                await SendMessageAsync(serverEndPoint, SurfaceHandShake.CreateMessage());
                _serverEndPoint = serverEndPoint;
                _timer = new Timer(async _ => await ConnectionCheckAsync(), null,
                    TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(10));
                _token.Register(_timer.Dispose);
            }
            catch (SocketException)
            {
                _logger.LogInformation($"{_serverEndPoint}: No response");
            }
        }

        //後にNetworkManager.csの同メソッドと統合します。
        async Task SendMessageAsync(IPEndPoint endPoint, Message message)
        {
            using var client = new TcpClient();
            await client.ConnectAsync(endPoint.Address, Surface.SURFACE_PORT);
            await using var stream = client.GetStream();
            await JsonSerializer.SerializeAsync(stream, message);
        }
    }
}
