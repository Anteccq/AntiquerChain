using System;
using System.Collections.Generic;
using System.Text;

namespace AntiquerChain.Blockchain
{
    public class Block
    {
        public HexString Id { get; set; }
        public uint Bits { get; set; }
        public HexString PreviousBlockHash { get; set; }
        public byte[] MerkleRootHash { get; set; }
        public DateTime Timestamp { get; set; }
        public ulong Nonce { get; set; }
        public List<Transaction> Transactions { get; set; }
    }
}
