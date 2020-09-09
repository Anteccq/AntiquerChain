using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using AntiquerChain.Blockchain;
using AntiquerChain.Cryptography;
using AntiquerChain.Mining;
using AntiquerChain.Network;
using ConsoleAppFramework;
using Microsoft.Extensions.Logging;
using Utf8Json;

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
            var (privateKey, publicKey) = SignManager.GenerateKeys();
            var publickKeyHash = new HexString(HashUtil.RIPEMD_SHA256(publicKey));
            //Genesis Mining
            var genesis = BlockchainManager.CreateGenesis();
            var miner = new Miner
            {
                MinerKeyHash = publickKeyHash
            };
            Console.WriteLine("Mining");
            miner.Mining(genesis, Context.CancellationToken);
            BlockchainManager.Chain.Add(genesis);

            //Second Block Mining
            var tb = new TransactionBuilder();
            var genTx = genesis.Transactions.First(x => x.Engraving == "");
            var input = new Input()
            {
                TransactionId = genTx.Id,
                OutputIndex = 0,
            };
            var output = new Output()
            {
                Amount = 10,
                PublicKeyHash = publickKeyHash.Bytes
            };
            tb.Inputs.Add(input);
            tb.Outputs.Add(output);
            var tx = tb.ToSignedTransaction(privateKey, publicKey);
            BlockchainManager.TransactionPool.Add(tx);
            miner.Start();


            Console.WriteLine("OK");
            Console.ReadLine();
        }
    }
}
