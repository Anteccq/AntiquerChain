using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using AntiquerChain.Cryptography;
using Utf8Json;

namespace AntiquerChain.Blockchain
{
    public class BlockchainManager
    {
        public static List<Block> Chain { get; } = new List<Block>();

        private const int CoinBaseInterval = 20;

        private static readonly DateTime GenesisTime = new DateTime(2020,8, 31, 15, 40, 30, DateTimeKind.Utc);

        public List<Transaction> TransactionPool { get; } = new List<Transaction>();

        public Transaction[] GetPool() => 
            TransactionPool.ToArray();

        public void ClearPool(int count) => 
            TransactionPool.RemoveRange(0, count);

        public void ClearPool(HexString[] strings)
        {
            TransactionPool.RemoveAll(x => strings.Contains(x.Id));
        }

        public static Block CreateGenesis()
        {
            var tx = CreateCoinBaseTransaction(0, null, "ArC - A Little BlockChain by C#");
            tx.TimeStamp = GenesisTime;
            var txs = new List<Transaction> {tx};
            var rootHash = HashUtil.ComputeMerkleRootHash(txs.Select(x => x.Id).ToList());

            return new Block()
            {
                PreviousBlockHash = new HexString(""),
                Nonce = 2083236893,
                Transactions = txs,
                MerkleRootHash = rootHash,
                Timestamp = GenesisTime
            };
        }

        public static Transaction CreateCoinBaseTransaction(int height, byte[] publicKeyHash, string engrave = "")
        {
            var tx = new Transaction()
            {
                TimeStamp = DateTime.UtcNow,
                Engraving = engrave,
                Inputs = new List<Input>(),
                Outputs = new List<Output>()
                {
                    new Output()
                    {
                        Amount = (ulong) GetSubsidy(height),
                        PublicKeyHash = publicKeyHash
                    }
                },
                TransactionFee = 0
            };
            var idBytes = JsonSerializer.Serialize(tx);
            tx.Id = new HexString(idBytes);
            return tx;
        }

        public bool VerifyBlockchain()
        {
            /*var i = 0;
            while (i < Blockchain.Count)
            {
                var rearData = JsonSerializer.Serialize(Blockchain[i]);
                var prevHash = Blockchain[i + 1].PreviousBlockHash.Bytes;
                if (prevHash != HashUtil.DoubleSHA256(rearData)) return false;
                i++;
            }*/

            var isRight = Chain.Take(Chain.Count - 1).SkipWhile((block, i) =>
            {
                var leadData = JsonSerializer.Serialize(block);
                return Chain[i + 1].PreviousBlockHash.Bytes != HashUtil.DoubleSHA256(leadData);
            }).Any();

            return !isRight;
        }

        public static int GetSubsidy(int height) =>
            50 >> height / CoinBaseInterval;

        public Block MakeBlock(ulong nonce, List<Transaction> transactions)
        {
            var merkleHash = HashUtil.ComputeMerkleRootHash(transactions.Select(x => x.Id).ToList());
            var lastBlock = JsonSerializer.Serialize(Chain.Last());
            return new Block()
            {
                MerkleRootHash = merkleHash,
                Nonce = nonce,
                PreviousBlockHash = new HexString(HashUtil.DoubleSHA256(lastBlock)),
                Timestamp = DateTime.UtcNow,
                Transactions = transactions
            };
        }
    }
}
