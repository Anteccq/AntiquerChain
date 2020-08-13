using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;

namespace AntiquerChain
{
    public static class EnumerableExtensions
    {
        public static IEnumerable<IPEndPoint> DistinctByAddress(this IEnumerable<IPEndPoint> list) =>
            list.GroupBy(x => x.Address).Select(g => g.First());
    }
}
