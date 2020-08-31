using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using AntiquerChain.Blockchain;
using AntiquerChain.Cryptography;
using Microsoft.Extensions.Logging;
using Utf8Json;

namespace AntiquerChain.Mining
{
    public class Miner
    {
        private ILogger _logger = Logging.Create<Miner>();

        public bool Mining(Block block, CancellationToken token)
        {
            var rnd = new Random();
            var buf = new byte[sizeof(ulong)];
            rnd.NextBytes(buf);

            var nonce = BitConverter.ToUInt64(buf, 0);
            while (!token.IsCancellationRequested)
            {
                block.Nonce = nonce++;
                block.Timestamp = DateTime.UtcNow;
                var data = JsonSerializer.Serialize(block);
                var hash = HashUtil.DoubleSHA256(data);
                _logger.LogInformation($"{BitConverter.ToDouble(hash)} : {BlockchainManager.DifficultyTarget}");
                if (BitConverter.ToDouble(hash) < BlockchainManager.DifficultyTarget) return true;
            }

            return false;
        }
    }
}
