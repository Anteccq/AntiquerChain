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

        public SurfaceManager(CancellationToken token, IPEndPoint endPoint)
        {
            _serverEndPoint = endPoint;
            var tokenSource = new CancellationTokenSource();
            _surface = new Surface(tokenSource, _serverEndPoint);
            _surface.MessageReceived += MessageHandle;
            _timer = new Timer(async _ => await ConnectionCheckAsync(), null,
                TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(10));
            token.Register(_timer.Dispose);
        }

        public async Task StartSurfaceAsync() => await (_surface?.StartAsync() ?? Task.CompletedTask);

        async Task ConnectionCheckAsync()
        {
            _logger.LogInformation("Connection checking..");
            try
            {
                await SendMessageAsync(_serverEndPoint, Ping.CreateMessage());
            }
            catch (SocketException)
            {
                _logger.LogInformation($"{_serverEndPoint} : No response.");
                _serverEndPoint = null;
            }
        }

        Task MessageHandle(Message msg)
        {
            _logger.LogInformation($"Message has arrived from {_serverEndPoint}. MSG_TYPE: {msg.Type} : {DateTime.Now:ss.FFFF}");
            return Task.CompletedTask;
        }
        public async Task ConnectAsync(IPEndPoint endPoint)
        {
            try
            {
                await SendMessageAsync(endPoint, SurfaceHandShake.CreateMessage());
            }
            catch (SocketException)
            {
                _logger.LogInformation($"{endPoint}: No response");
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
