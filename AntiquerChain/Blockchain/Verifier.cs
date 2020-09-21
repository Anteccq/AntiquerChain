using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using AntiquerChain.Blockchain.Util;
using AntiquerChain.Mining;

namespace AntiquerChain.Blockchain
{
    public class Verifier
    {
        public event Action Applied;

        public void Verify(Block block)
        {
            lock (BlockchainManager.Chain)
            {
                BlockchainManager.Chain.Add(block);
            }

            lock (BlockchainManager.TransactionPool)
            {
                foreach (var tx in block.Transactions)
                {
                    BlockchainManager.TransactionPool.RemoveAll(x => x.Id.Bytes.IsEqual(tx.Id.Bytes));
                }
            }

            Applied?.Invoke();
        }
    }
}
