using System;
using System.Collections.Generic;
using System.Text;

namespace AntiquerChain.Blockchain
{
    public class Transaction
    {
        public HexString Id { get; set; }
        public DateTime TimeStamp { get; set; }
        public string Engraving { get; set; }
        public List<Output> Outputs { get; set; }
        public List<Input> Inputs { get; set; }
        public ulong TransactionFee { get; set; }
    }

    public class Output
    {
        public ulong Amount;
        public byte[] PublicKeyHash { get; set; }
    }

    public class Input
    {
        public HexString TransactionId { get; set; }
        public int OutputIndex { get; set; }
        public byte[] Signature { get; set; }
        public byte[] PublicKey { get; set; }
    }
}
