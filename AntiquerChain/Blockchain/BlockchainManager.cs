using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using AntiquerChain.Cryptography;
using Utf8Json;

namespace AntiquerChain.Blockchain
{
    public class BlockchainManager
    {
        public List<Block> Blockchain { get; } = new List<Block>();
        private const int CoinBaseInterval = 20;

        public static Block CreateGenesis()
        {
            var txs = new List<Transaction>()
            {
                CreateCoinBaseTransaction(0, null, "ArC - A Little BlockChain by C#")
            };
            var rootHash = HashUtil.ComputeMerkleRootHash(txs.Select(x => x.Id).ToList());

            return new Block()
            {
                PreviousBlockHash = new HexString(""),
                Nonce = 2083236893,
                Transactions = txs,
                MerkleRootHash = rootHash,
                Timestamp = new DateTime(2020,8,27),
            };
        }

        public static Transaction CreateCoinBaseTransaction(int height, byte[] publicKeyHash, string engrave = "")
        {
            var tx = new Transaction()
            {
                TimeStamp = new DateTime(2020, 8, 27),
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

        public static int GetSubsidy(int height) =>
            50 >> height / CoinBaseInterval;

        public Block MakeBlock(HexString previousHash, int nonce, List<Transaction> transactions)
        {
            var merkleHash = HashUtil.ComputeMerkleRootHash(transactions.Select(x => x.Id).ToList());
            return new Block()
            {
                MerkleRootHash = merkleHash,
                Nonce = nonce,
                PreviousBlockHash = previousHash,
                Timestamp = DateTime.UtcNow,
                Transactions = transactions
            };
        }
    }
}
