using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using AntiquerChain.Blockchain;
using AntiquerChain.Cryptography;
using AntiquerChain.Network;
using Microsoft.Extensions.Logging;
using Utf8Json;

namespace AntiquerChain.Mining
{
    public class Miner
    {
        private ILogger _logger = Logging.Create<Miner>();
        public bool IsMining = false;
        private CancellationTokenSource _tokenSource;
        public NetworkManager NetworkManager { get; set; }

        public HexString MinerKeyHash { get; set; }

        public static bool Mining(Block block, CancellationToken token)
        {
            var rnd = new Random();
            var buf = new byte[sizeof(ulong)];
            rnd.NextBytes(buf);
            var target = Difficulty.GetTargetBytes(block.Bits);
            var nonce = BitConverter.ToUInt64(buf, 0);
            while (!token.IsCancellationRequested)
            {
                block.Nonce = nonce++;
                block.Timestamp = DateTime.UtcNow;
                var data = JsonSerializer.Serialize(block);
                var hash = HashUtil.DoubleSHA256(data);
                if (!HashCheck(hash, target)) continue;
                block.Id = new HexString(hash);
                return true;
            }
            return false;
        }

        public static bool HashCheck(byte[] data1, byte[] target)
        {
            if (data1.Length != 32 || target.Length != 32) return false;
            for (var i = 0; i < data1.Length; i++)
            {
                if (data1[i] < target[i]) return true;
                if (data1[i] > target[i]) return false;
            }
            return true;
        }

        public void Start()
        {
            _tokenSource = new CancellationTokenSource();
            IsMining = true;
            Task.Run(() => Execute(_tokenSource.Token));
        }

        public void Stop()
        {
            IsMining = false;
            if (_tokenSource is null) return;

            _tokenSource.Cancel();
            _tokenSource.Dispose();
            _tokenSource = null;
        }

        public void Restart()
        {
            if(!IsMining) return;
            Stop();
            Start();
        }

        public void Execute(CancellationToken token)
        {
            var txs = BlockchainManager.GetPool();
            var time = DateTime.UtcNow;
            var subsidy = BlockchainManager.GetSubsidy(BlockchainManager.Chain.Count);

            var txList = txs.Where(tx =>
            {
                try
                {
                    token.ThrowIfCancellationRequested();
                    BlockchainManager.VerifyTransaction(tx, time);
                    subsidy += tx.TransactionFee;
                    return true;
                }
                catch
                {
                    return false;
                }
            }).ToList();

            var cbOut = new Output()
            {
                Amount = subsidy,
                PublicKeyHash = MinerKeyHash.Bytes
            };
            var tb = new TransactionBuilder(new List<Output>(){cbOut}, new List<Input>());
            var coinbaseTx = tb.ToTransaction(time);

            BlockchainManager.VerifyTransaction(coinbaseTx, time, subsidy);
            txList.Insert(0, coinbaseTx);

            var txIds = txList.Select(x => x.Id).ToList();
            
            var block = new Block()
            {
                Id = null,
                PreviousBlockHash = BlockchainManager.Chain.Last().Id,
                Transactions = txList,
                MerkleRootHash = HashUtil.ComputeMerkleRootHash(txIds),
                Bits = Difficulty.DifficultyBits
            };
            
            if (!Mining(block, token))
            {
                _logger.LogError($"Error. Stop Mining");
                return;
            }

            _logger.LogInformation($"Success : {string.Join("", block.Id.Bytes.Select(x => $"{x:X2}"))}");

            _logger.LogInformation($"Mined");
            _logger.LogInformation($"{JsonSerializer.PrettyPrint(JsonSerializer.Serialize(block))}");

            //Broadcast Block
            var msg = NewBlock.CreateMessage(block);
            NetworkManager.BroadCastMessageAsync(msg);
            //NetworkManager.NewBlockHandle(new NewBlock() {Block = block}, null);
            _logger.LogInformation($"End Mine");
        }
    }
}
