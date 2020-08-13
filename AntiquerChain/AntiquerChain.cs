using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using AntiquerChain.Network;
using ConsoleAppFramework;
using Microsoft.Extensions.Logging;

namespace AntiquerChain
{
    public class AntiquerChain : ConsoleAppBase
    {
        [Command("run", "Start P2P Network")]
        public async Task RunAsync([Option("i")] string endPoint)
        {
            if (string.IsNullOrEmpty(endPoint) || !IPEndPoint.TryParse(endPoint, out var ipEndPoint))
            {
                Console.WriteLine("異常終了");
                return;
            }
            var manager = new NetworkManager(Context.CancellationToken);
            await manager.StartServerAsync();
            var t = Task.Run(async () => await manager.ConnectAsync(ipEndPoint), Context.CancellationToken);

            Console.ReadLine();
        }
    }
}
