using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using AntiquerChain.Blockchain.Util;
using AntiquerChain.Mining;
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
            _logger.LogInformation($"Block Verified! Restart Mining.");
            Applied?.Invoke();
        }
    }
}
