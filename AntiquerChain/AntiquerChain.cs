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
using static AntiquerChain.Network.Util.Messenger;
namespace AntiquerChain
{
    public class AntiquerChain : ConsoleAppBase
    {
        [Command("run", "Start P2P Network")]
        public async Task RunAsync([Option("i")] string endPoint)
        {
            var (privateKey, publicKey) = SignManager.GenerateKeys();
            var publickKeyHash = new HexString(HashUtil.RIPEMD_SHA256(publicKey));
            if (string.IsNullOrEmpty(endPoint) || !IPEndPoint.TryParse(endPoint, out var ipEndPoint))
            {
                Console.WriteLine("異常終了");
                return;
            }
            var vry = new Verifier();
            var miner = new Miner
            {
                MinerKeyHash = publickKeyHash
            };
            var manager = new NetworkManager(Context.CancellationToken, vry);
            await manager.StartServerAsync();
            var t = Task.Run(async () => await manager.ConnectAsync(ipEndPoint), Context.CancellationToken);
            var surfaceManager = new SurfaceManager(Context.CancellationToken);
            surfaceManager.StartSurface();
            var tt = Task.Run(async () => await surfaceManager.ConnectServerAsync(ipEndPoint), Context.CancellationToken);

            Console.ReadLine();
        }

        [Command("min", "Mining")]
        public async Task Mining()
        {
            var (privateKey, publicKey) = SignManager.GenerateKeys();
            var publickKeyHash = new HexString(HashUtil.RIPEMD_SHA256(publicKey));
            //Genesis Mining
            var genesis = BlockchainManager.CreateGenesis();
            var miner = new Miner
            {
                MinerKeyHash = publickKeyHash
            };
            var verifier = new Verifier();
            verifier.Applied += () => miner.Restart();
            var nm = new NetworkManager(Context.CancellationToken, verifier);
            nm.Miner = miner;
            miner.NetworkManager = nm;
            Console.WriteLine("Mining");
            Miner.Mining(genesis, Context.CancellationToken);
            BlockchainManager.Chain.Add(genesis);


            for (var i = 0; i < 10; i++)
            {
                var gg = BlockchainManager.CreateCoinBaseTransaction(i+1, publickKeyHash.Bytes, $"まかろに{i}");
                gg.TimeStamp = DateTime.UtcNow;
                var txs = new List<Transaction>() { gg };
                var rootHash = HashUtil.ComputeMerkleRootHash(txs.Select(x => x.Id).ToList());

                var b = new Block()
                {
                    PreviousBlockHash = BlockchainManager.Chain.Last().Id,
                    Transactions = txs,
                    MerkleRootHash = rootHash,
                    Timestamp = DateTime.UtcNow,
                    Bits = 12
                };
                Miner.Mining(b, Context.CancellationToken);
                var bm = new NewBlock(){Block = b};
                await nm.NewBlockHandle(bm, null);
                Task.Delay(10).GetAwaiter().GetResult();
            }

            //Second Block Mining
            Console.WriteLine($"{genesis.Transactions.Count}");
            var tb = new TransactionBuilder();
            var ttx = BlockchainManager.Chain.SelectMany(x => x.Transactions).First(x => x.Engraving == "まかろに0");

            var input = new Input()
            {
                TransactionId = ttx.Id,
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

            Console.ReadLine();
            miner.Stop();
            Console.WriteLine($"{BlockchainManager.VerifyBlockchain()} : OK");
        }

        [Command("runmin", "Run Network and Mining")]
        public async Task RunAndMining([Option("i")] string endPoint, [Option("f")] bool isFirst)
        {
            if (string.IsNullOrEmpty(endPoint) || !IPEndPoint.TryParse(endPoint, out var ipEndPoint))
            {
                Console.WriteLine("異常終了");
                return;
            }
            var (privateKey, publicKey) = SignManager.GenerateKeys();
            var publickKeyHash = new HexString(HashUtil.RIPEMD_SHA256(publicKey));
            //Genesis Mining
            var genesis = BlockchainManager.CreateGenesis();
            var miner = new Miner
            {
                MinerKeyHash = publickKeyHash
            };
            var verifier = new Verifier();
            verifier.Applied += () => miner.Restart();
            var nm = new NetworkManager(Context.CancellationToken, verifier);
            nm.Miner = miner;
            miner.NetworkManager = nm;

            await nm.StartServerAsync();
            var t = Task.Run(async () => await nm.ConnectAsync(ipEndPoint), Context.CancellationToken);
            var surfaceManager = new SurfaceManager(Context.CancellationToken);
            surfaceManager.StartSurface();
            var tt = Task.Run(async () => await surfaceManager.ConnectServerAsync(ipEndPoint), Context.CancellationToken);

            if (isFirst)
            {
                Console.WriteLine("Genesis Mining");
                Miner.Mining(genesis, Context.CancellationToken);
                BlockchainManager.Chain.Add(genesis);
                miner.Start();
            }
            else
            {
                t.Wait();
                await SendMessageAsync(ipEndPoint.Address, NetworkConstant.SERVER_PORT,
                    new Message() {Type = MessageType.RequestFullChain});
            }


            Console.ReadLine();
        }
    }
}
