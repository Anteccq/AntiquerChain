using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AntiquerChain.Blockchain.Util
{
    public static class ByteArrayExtensions
    {
        public static bool IsEqual(this byte[] data1, byte[] data2)
        {
            try
            {
                return !data1.Where((t, i) => t != data2[i]).Any();
            }
            catch { return false; }
        }
    }
}
