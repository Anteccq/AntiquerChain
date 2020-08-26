using System;
using System.Collections.Generic;
using System.Text;
using AntiquerChain.Blockchain;
using Utf8Json;

namespace AntiquerChain.Formatter
{
    public class HexStringFormatter : IJsonFormatter<HexString>
    {
        public void Serialize(ref JsonWriter writer, HexString value, IJsonFormatterResolver formatterResolver)
        {
            if (value == null) { writer.WriteNull(); return; }

            formatterResolver.GetFormatterWithVerify<string>().Serialize(ref writer, value.String ?? "", formatterResolver);
        }

        public HexString Deserialize(ref JsonReader reader, IJsonFormatterResolver formatterResolver)
        {
            if (reader.ReadIsNull()) return null;

            var str = formatterResolver.GetFormatterWithVerify<string>().Deserialize(ref reader, formatterResolver);
            return new HexString(str ?? "");
        }
    }
}
