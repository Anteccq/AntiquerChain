using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using AntiquerChain.Blockchain;
using AntiquerChain.Mining;
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
            var surfaceManager = new SurfaceManager(Context.CancellationToken);
            surfaceManager.StartSurface();
            var tt = Task.Run(async () => await surfaceManager.ConnectServerAsync(ipEndPoint), Context.CancellationToken);

            Console.ReadLine();
        }

        [Command("min", "Mining genesis block")]
        public void Mining()
        {
            var genesis = BlockchainManager.CreateGenesis();
            var miner = new Miner();
            Console.WriteLine("Calculate");
            BlockchainManager.DifficultyTarget *= 1000;
            Console.WriteLine($"{BlockchainManager.DifficultyTarget}");
            var by = BitConverter.GetBytes(BlockchainManager.DifficultyTarget).Reverse().ToArray();
            Console.WriteLine(string.Join("", by.Select(x => $"{x:X2}")));
            miner.Mining(genesis, Context.CancellationToken);
            Console.WriteLine("OK");
            Console.ReadLine();
        }
    }
}
