using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using AntiquerChain.Blockchain.Util;
using AntiquerChain.Cryptography;
using Utf8Json;

namespace AntiquerChain.Blockchain
{
    public static class BlockchainManager
    {
        public static List<Block> Chain { get; } = new List<Block>();

        private const int CoinBaseInterval = 20;

        private static readonly DateTime GenesisTime = new DateTime(2020,8, 31, 15, 40, 30, DateTimeKind.Utc);

        public static List<Transaction> TransactionPool { get; } = new List<Transaction>();

        public static Transaction[] GetPool() => 
            TransactionPool.ToArray();

        public static void ClearPool(int count)
        {
            lock (TransactionPool)
            {
                TransactionPool.RemoveRange(0, count);
            }
        }

        public static void ClearPool(string ids)
        {
            lock (TransactionPool)
            {
                TransactionPool.RemoveAll(x => ids.Contains(x.Id.String));
            }
        }

        public static void AddTx(Transaction tx)
        {
            lock (TransactionPool)
            {
                if(TransactionPool.All(x => x.Id.Bytes != tx.Id.Bytes)) return;
                TransactionPool.Add(tx);
            }
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
                Timestamp = GenesisTime,
                Bits = 5
            };
        }

        public static Transaction CreateCoinBaseTransaction(int height, byte[] publicKeyHash, string engrave = "")
        {
            var cbOut = new Output()
            {
                Amount = (ulong) GetSubsidy(height),
                PublicKeyHash = publicKeyHash
            };
            var tb = new TransactionBuilder(new List<Output>(){cbOut}, new List<Input>(), engrave);
            return tb.ToTransaction();
        }

        public static bool VerifyBlockchain()
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

        public static ulong GetSubsidy(int height) =>
            (ulong)50 >> height / CoinBaseInterval;

        public static Block MakeBlock(ulong nonce, List<Transaction> transactions)
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

        public static void VerifyTransaction(Transaction tx, DateTime timestamp, ulong coinbase = 0)
        {
            if(tx.TimeStamp > timestamp ||
               !(coinbase == 0 ^ tx.Inputs.Count == 0))
                throw new ArgumentException();

            var hash = HashUtil.ComputeTransactionSignHash(JsonSerializer.Serialize(tx));
            //Input check
            var inSum = coinbase;
            foreach (var input in tx.Inputs)
            {
                var chainTxs = Chain.SelectMany(x => x.Transactions);
                //Input Verify
                var transactions = chainTxs as Transaction[] ?? chainTxs.ToArray();
                var prevOutTx = transactions
                        .First(x => x.Id.Bytes == input.TransactionId.Bytes)?
                        .Outputs[input.OutputIndex];
                var verified = prevOutTx != null && SignManager.Verify(hash, input.Signature, input.PublicKey, prevOutTx.PublicKeyHash);

                //utxo check ブロックの長さに比例してコストが上がってしまう問題アリ
                var utxoUsed  = transactions.SelectMany(x => x.Inputs).Any(ipt => ipt.TransactionId.Bytes != input.TransactionId.Bytes);

                var redeemable = prevOutTx.PublicKeyHash.IsEqual(HashUtil.RIPEMD_SHA256(input.PublicKey));

                inSum = checked(inSum + prevOutTx.Amount);

                if(!verified || utxoUsed || !redeemable)
                    throw new ArgumentException();
            }

            ulong outSum = 0;
            foreach (var output in tx.Outputs)
            {
                if (output.PublicKeyHash is null || output.Amount <= 0)
                    throw new ArgumentException();
                outSum = checked(outSum + output.Amount);
            }

            if(outSum > inSum) throw new ArgumentException();

            tx.TransactionFee = inSum - outSum;
        }
    }
}
