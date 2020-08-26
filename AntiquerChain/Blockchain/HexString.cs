using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Org.BouncyCastle.Utilities.Encoders;

namespace AntiquerChain.Blockchain
{
    public class HexString
    {
        private string _string;
        private byte[] _bytes;

        public string String
        {
            get => _string;
            set
            {
                _string = value;
                _bytes = ToBytes(value);
            }
        }

        public byte[] Bytes
        {
            get => _bytes;
            set
            {
                _bytes = value;
                _string = HexToString(value);
            }
        }

        public HexString(string str)
        {
            String = str;
        }

        public HexString(byte[] bytes)
        {
            Bytes = bytes;
        }

        public static byte[] ToBytes(string s)
        {
            var str = s;
            var array = new byte[str.Length / 2];
            for (var i = 0; i < str.Length; i += 2)
            {
                array[i / 2] = Convert.ToByte(str.Substring(i, 2), 16);
            }
            return array;
        }

        public static string HexToString(byte[] data) =>
            string.Join("",data.Select(x => $"{x:X2}"));

        public override string ToString() => String;
    }
}
