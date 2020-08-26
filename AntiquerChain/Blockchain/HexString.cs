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
                ToBytes();
            }
        }

        public byte[] Bytes
        {
            get => _bytes;
            set
            {
                _bytes = value;
                HexToString();
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

        private void ToBytes()
        {
            var str = _string;
            var array = new byte[str.Length / 2];
            for (var i = 0; i < str.Length; i += 2)
            {
                array[i / 2] = Convert.ToByte(str.Substring(i, 2), 16);
            }
            _bytes = array;
        }

        private void HexToString()
        {
            _string = string.Join("",_bytes.Select(x => $"{x:X2}"));
        }

        public override string ToString() => String;
    }
}
