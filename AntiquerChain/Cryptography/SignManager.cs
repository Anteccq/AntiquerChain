using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using Org.BouncyCastle.Asn1.Sec;
using Org.BouncyCastle.Math;
using Org.BouncyCastle.Security;
using Utf8Json;

namespace AntiquerChain.Cryptography
{
    public class SignManager
    {
        private const string CurveName = "secp256k1";

        public static readonly ECCurve Secp256k1Curve = ECCurve.CreateFromValue("1.3.132.0.10"); //secp256k1 from OID

        public static (byte[] privateKey, byte[] publicKey) GenerateKeys()
        {
            var rnd = new SecureRandom();
            rnd.SetSeed(DateTime.UtcNow.Ticks);

            var c = SecNamedCurves.GetByName(CurveName);
            var bi = GeneratePrivateKey(c.N, rnd);
            var privateKey = bi.ToByteArrayUnsigned();

            var point = c.G.Multiply(bi).Normalize();

            var publicKey = JsonSerializer.Serialize(new Coordinate()
            {
                X = point.XCoord.GetEncoded(),
                Y = point.YCoord.GetEncoded()
            });

            return (privateKey, publicKey);
        }

        public static byte[] Signature(byte[] hash, byte[] privateKey, byte[] publicKey)
        {
            var (x, y) = JsonSerializer.Deserialize<Coordinate>(publicKey);
            var pubEcPoint = new ECPoint(){X = x, Y = y};
            var ecp = new ECParameters()
            {
                D = privateKey,
                Q = pubEcPoint,
                Curve = Secp256k1Curve
            };
            using var ecdsa = ECDsa.Create(ecp);
            return ecdsa.SignHash(hash);
        }

        public static bool Verify(byte[] hash, byte[] signature, byte[] publicKey)
        {
            var (x, y) = JsonSerializer.Deserialize<Coordinate>(publicKey);
            var pubEcPoint = new ECPoint() { X = x, Y = y };
            var ecp = new ECParameters()
            {
                Q = pubEcPoint,
                Curve = Secp256k1Curve
            };
            using var ecdsa = ECDsa.Create(ecp);
            return ecdsa.VerifyHash(hash, signature);
        }

        private static BigInteger GeneratePrivateKey(BigInteger n, SecureRandom rnd)
        {
            while (true)
            {
                var bi = new BigInteger(n.BitLength, rnd).SetBit(n.BitLength - 1);
                if (bi.CompareTo(n) < 0) return bi;
            }
        }

        public class Coordinate
        {
            public byte[] X { get; set; }

            public byte[] Y { get; set; }

            public void Deconstruct(out byte[] x, out byte[] y) => (x, y) = (X, Y);
        }
    }
}
