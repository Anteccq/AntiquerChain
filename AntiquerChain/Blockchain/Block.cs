using System;
using System.Collections.Generic;
using System.Text;

namespace AntiquerChain.Blockchain
{
    public class Block
    {
        public HexString PreviousBlockHash { get; set; }
        public byte[] MerkleRootHash { get; set; }
        public DateTime Timestamp { get; set; }
        public int Nonce { get; set; }
        public List<Transaction> Transactions { get; set; }
    }
}
