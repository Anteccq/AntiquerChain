using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using AntiquerChain.Blockchain;
using Org.BouncyCastle.Crypto.Digests;
using Utf8Json;

namespace AntiquerChain.Cryptography
{
    public static class HashUtil
    {
        public static byte[] RIPEMD160(byte[] data)
        {
            var digest = new RipeMD160Digest();
            var result = new byte[digest.GetDigestSize()];
            digest.BlockUpdate(data, 0, data.Length);
            digest.DoFinal(result, 0);
            return result;
        }

        public static byte[] SHA256(byte[] data)
        {
            using var sha256 = System.Security.Cryptography.SHA256.Create();
            return sha256.ComputeHash(data);
        }

        public static byte[] DoubleSHA256(byte[] data)
        {
            using var sha256 = System.Security.Cryptography.SHA256.Create();
            return sha256.ComputeHash(sha256.ComputeHash(data));
        }

        public static byte[] RIPEMD_SHA256(byte[] data) => RIPEMD160(SHA256(data));

        public static byte[] ComputeMerkleRootHash(IList<HexString> leaves) =>
            ComputeMerkleRootHash(leaves.Select(x => x.Bytes).ToList());

        public static byte[] ComputeMerkleRootHash(IList<byte[]> bytes)
        {
            while (true)
            {
                if (bytes.Count == 1) return bytes.First();

                if (bytes.Count % 2 > 0) bytes.Add(bytes.Last());
                var blanches = new List<byte[]>();
                for (var i = 0; i < bytes.Count; i += 2)
                {
                    blanches.Add(DoubleSHA256(bytes[i].Concat(bytes[i + 1]).ToArray()));
                }

                bytes = blanches;
            }
        }

        public static byte[] ComputeTransactionSignHash(byte[] data)
        {
            var tx = JsonSerializer.Deserialize<Transaction>(data);
            foreach (var input in tx.Inputs)
            {
                input.PublicKey = null;
                input.Signature = null;
            }

            var b = JsonSerializer.Serialize(tx);
            return HashUtil.DoubleSHA256(b);
        }
    }
}
