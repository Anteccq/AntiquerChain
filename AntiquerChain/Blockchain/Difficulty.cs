using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using AntiquerChain.Blockchain;
using static AntiquerChain.Blockchain.BlockchainManager;

namespace AntiquerChain.Blockchain
{
    public static class Difficulty
    {
        private static uint _difficultyBits = MinDifficultBits;
        public static uint DifficultyBits
        {
            get => _difficultyBits;
            set
            {
                if (value > MaxDifficultBits) value = MaxDifficultBits;
                if (value < MinDifficultBits) value = MinDifficultBits;
                _difficultyBits = value;
            }
        }
        public const uint MaxDifficultBits = 32;
        public const uint MinDifficultBits = 5;

        public static byte[] TargetBytes
        {
            get
            {
                var target = (BigInteger)Math.Pow(2, 0xFF - DifficultyBits);
                var bytes = target.ToByteArray(true, true);
                return new byte[32-bytes.Length].Concat(bytes).ToArray();
            }
        }

        private const int TargetTime = 6000;
        private const int DifInterval = 100;

        public static void CalculateNextDifficulty()
        {
            var actualTime = (Chain.Last().Timestamp - Chain[^DifInterval].Timestamp).Seconds;
            if (actualTime < TargetTime / 2) DifficultyBits++;
            if (actualTime > TargetTime * 2) DifficultyBits--;
        }
    }
}
