using System;
using System.Collections.Generic;
using System.Linq;
using AntiquerChain.Blockchain.Util;
using Microsoft.Extensions.Logging;
using static AntiquerChain.Blockchain.BlockchainManager;

namespace AntiquerChain.Blockchain
{
    public class Verifier
    {
        public event Action Applied;
        private ILogger _logger = Logging.Create<Verifier>();

        public void Verify(Block block)
        {
            lock (Chain)
            {
                Chain.Add(block);
            }

            lock (TransactionPool)
            {
                foreach (var tx in block.Transactions)
                {
                    TransactionPool.RemoveAll(x => x.Id.Bytes.IsEqual(tx.Id.Bytes));
                }
            }
            if(Chain.Count % 100 == 0) Difficulty.CalculateNextDifficulty();

            //Mining restart
            _logger.LogInformation("Block Verified! Restart Mining.");
            Applied?.Invoke();
        }

        public void ChainApply(List<Block> chain)
        {
            lock(this) LockedChainApply(chain);
        }

        void LockedChainApply(List<Block> chain)
        {
            _logger.LogInformation($"Chain Applying");
            var localTxs = Chain.SelectMany(x => x.Transactions);
            var remoteTxs = chain.SelectMany(x => x.Transactions);
            localTxs = localTxs.Where(tx => !remoteTxs.Any(x => x.Id.Bytes.IsEqual(tx.Id.Bytes))).ToList();
            _logger.LogInformation("on Full Chain Tx Remove.");
            lock (TransactionPool)
            {
                foreach (var tx in TransactionPool.Where(tx => remoteTxs.Any(x => x.Id.Bytes.IsEqual(tx.Id.Bytes))))
                {
                    TransactionPool.Remove(tx);
                }
                TransactionPool.AddRange(localTxs);
            }
            _logger.LogInformation("on Full Chain Applied Chain.");
            lock (Chain)
            {
                Chain.Clear();
                Chain.AddRange(chain);
            }
        }
    }
}
