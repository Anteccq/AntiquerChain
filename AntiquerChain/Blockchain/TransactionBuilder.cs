using System;
using System.Collections.Generic;
using System.Text;
using AntiquerChain.Cryptography;
using Utf8Json;

namespace AntiquerChain.Blockchain
{
    public class TransactionBuilder
    {
        private readonly Transaction _transaction;

        public IList<Output> Outputs => _transaction.Outputs;

        public IList<Input> Inputs => _transaction.Inputs;

        public TransactionBuilder() : this(new Transaction())
        {
            
        }

        public TransactionBuilder(List<Output> outputs, List<Input> inputs, string engrave = "")
        {
            _transaction = new Transaction()
            {
                Engraving = engrave,
                Outputs = outputs,
                Inputs = inputs
            };
        }

        public TransactionBuilder(Transaction tx)
        {
            tx.Inputs ??= new List<Input>();
            tx.Outputs ??= new List<Output>();
            tx.TransactionFee = 0;
            _transaction = tx;
        }

        public Transaction ToSignedTransaction(byte[] privateKey, byte[] publicKey)
        {
            _transaction.TimeStamp = DateTime.UtcNow;
            _transaction.Id = null;
            var hash = HashUtil.ComputeTransactionSignHash(JsonSerializer.Serialize(_transaction));
            var signature = SignManager.Signature(hash, privateKey, publicKey);
            foreach (var inEntry in Inputs)
            {
                inEntry.PublicKey = publicKey;
                inEntry.Signature = signature;
            }
            var txData = JsonSerializer.Serialize(_transaction);
            var txHash = HashUtil.DoubleSHA256(txData);
            _transaction.Id = new HexString(txHash);
            return _transaction;
        }

        public Transaction ToTransaction()
        {
            _transaction.TimeStamp = DateTime.UtcNow;
            var txData = JsonSerializer.Serialize(_transaction);
            var txHash = HashUtil.DoubleSHA256(txData);
            _transaction.Id = new HexString(txHash);
            return _transaction;
        }
    }
}
