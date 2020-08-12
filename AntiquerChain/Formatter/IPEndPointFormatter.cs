using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using Utf8Json;

namespace AntiquerChain.Formatter
{
    public class IPEndPointFormatter : IJsonFormatter<IPEndPoint>
    {
        public void Serialize(ref JsonWriter writer, IPEndPoint value, IJsonFormatterResolver formatterResolver)
        {
            if (value == null) { writer.WriteNull(); return; }

            formatterResolver.GetFormatterWithVerify<string>().Serialize(ref writer, $"{value}", formatterResolver);
        }

        public IPEndPoint Deserialize(ref JsonReader reader, IJsonFormatterResolver formatterResolver)
        {
            if (reader.ReadIsNull()) return null;

            var str = formatterResolver.GetFormatterWithVerify<string>().Deserialize(ref reader, formatterResolver);
            return !IPEndPoint.TryParse(str, out var endPoint) ? null : endPoint;
        }
    }
}
