using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Extensions.Logging;

namespace AntiquerChain
{
    public static class Logging
    {
        //Taken by https://docs.microsoft.com/en-us/archive/msdn-magazine/2016/april/essential-net-logging-with-net-core
        public static ILoggerFactory Factory { get; } = new LoggerFactory();
        public static ILogger Create<T>() =>
            Factory.CreateLogger<T>();
    }
}
