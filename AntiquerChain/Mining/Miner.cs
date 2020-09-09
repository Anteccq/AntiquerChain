using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using AntiquerChain.Blockchain;
using AntiquerChain.Cryptography;
using Microsoft.Extensions.Logging;
using Utf8Json;

namespace AntiquerChain.Mining
{
    public class Miner
    {
        private ILogger _logger = Logging.Create<Miner>();
        public bool IsMining = false;
        private CancellationTokenSource _tokenSource;

        public HexString MinerKeyHash { get; set; }

        public bool Mining(Block block, CancellationToken token)
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
                _logger.LogInformation($"{string.Join("", hash.Select(x => $"{x:X2}"))}");
                if (!HashCheck(hash, target)) continue;
                _logger.LogInformation($"Success : {string.Join("",hash.Select(x => $"{x:X2}"))}");
                block.Id = new HexString(hash);
                return true;
            }

            return false;
        }

        private static bool HashCheck(byte[] data1, byte[] target)
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
            Execute(_tokenSource.Token);
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

            var coinbaseTx = new Transaction()
            {
                Inputs = new List<Input>(),
                Outputs = new List<Output>() {new Output() {Amount = subsidy, PublicKeyHash = MinerKeyHash.Bytes}},
                TimeStamp = time
            };
            coinbaseTx.Id = new HexString(HashUtil.DoubleSHA256(JsonSerializer.Serialize(coinbaseTx)));
            BlockchainManager.VerifyTransaction(coinbaseTx, time, subsidy);
            txList.Insert(0, coinbaseTx);

            var txIds = txList.Select(x => x.Id).ToList();
            var block = new Block()
            {
                PreviousBlockHash = BlockchainManager.Chain.Last().Id,
                Transactions = txList,
                MerkleRootHash = HashUtil.ComputeMerkleRootHash(txIds),
                Bits = Difficulty.DifficultyBits
            };

            if(!Mining(block, token)) return;

            _logger.LogInformation($"Mined : {JsonSerializer.ToJsonString(block)}");

            //Broadcast Block
        }
    }
}
